using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Models.Item;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using GMS.TifoXRCoreWebAPI.Repositories.Sql;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;
using Nethereum.Signer.Crypto;
using System.Data.Common;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed class ItemRepository : IItemRepository
    {
        private readonly IDbProvider _db;
        public ItemRepository(IDbProvider db) => _db = db;

        public async Task<HashSet<string>> GetSupportedLocalesAsync(int spaceId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ItemSql.GetSupportedLocales);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync()) set.Add(rdr.GetString(0));
            return set;
        }

        public async Task<bool> RegionalCurrencyExistsAsync(int regionalCurrencyId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ItemSql.ExistsRegionalCurrency);
            cmd.Parameters.Add(_db.CreateParameter("@RegionalCurrencyId", regionalCurrencyId));
            return (await cmd.ExecuteScalarAsync()) is not null;
        }

        public async Task<ItemCreatedEnvelope> AddAsync(int spaceId, CreateItemDto dto, string modifiedBy)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // A) Insert item
                long newItemId;
                await using (var ci = _db.CreateCommand(conn, ItemSql.InsertItem, tx))
                {
                    ci.Parameters.Add(_db.CreateParameter("@ItemTypeId", dto.ItemTypeId));
                    ci.Parameters.Add(_db.CreateParameter("@EntityId", (object?)dto.EntityId ?? DBNull.Value));
                    ci.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    ci.Parameters.Add(_db.CreateParameter("@NameKey", dto.NameKey));
                    ci.Parameters.Add(_db.CreateParameter("@DescriptionKey", (object?)dto.DescriptionKey ?? DBNull.Value));
                    ci.Parameters.Add(_db.CreateParameter("@IsAvailable", dto.IsAvailable ? 1 : 0));
                    ci.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                    newItemId = Convert.ToInt64(await ci.ExecuteScalarAsync());
                }

                // B) i18n upserts (if provided)
                if (dto.NameLocalizedPairs?.Values?.Count > 0)
                    await UpsertI18nAsync(spaceId, dto.NameKey, dto.NameLocalizedPairs.Values, modifiedBy, conn, tx);

                if (!string.IsNullOrWhiteSpace(dto.DescriptionKey) && dto.DescriptionLocalizedPairs?.Values?.Count > 0)
                    await UpsertI18nAsync(spaceId, dto.DescriptionKey!, dto.DescriptionLocalizedPairs.Values, modifiedBy, conn, tx);


                var supportedLocales = await GetSupportedLocalesInternalAsync(conn, tx, spaceId);

                // C) subtype
                switch (dto.ItemTypeId)
                {
                    case 1: // Merchandise
                        {
                            string? thumbId = dto.Merchandise?.ThumbnailMediaId;

                            if (thumbId is null && dto.Merchandise?.ThumbnailMedia is not null)
                            {
                                thumbId = await FilterAndInsertMediaAsync(
                                    conn, tx, spaceId, supportedLocales,
                                    dto.Merchandise.ThumbnailMedia, modifiedBy);
                            }

                            await using var cm = _db.CreateCommand(conn, ItemSql.InsertMerchandise, tx);
                            cm.Parameters.Add(_db.CreateParameter("@ItemId", newItemId));
                            cm.Parameters.Add(_db.CreateParameter("@MerchTypeId", dto.Merchandise!.MerchandiseTypeId));
                            cm.Parameters.Add(_db.CreateParameter("@MerchCatId", (object?)dto.Merchandise.MerchandiseCategoryId ?? DBNull.Value));
                            cm.Parameters.Add(_db.CreateParameter("@AssetUrl", (object?)dto.Merchandise.AssetUrl ?? DBNull.Value));
                            cm.Parameters.Add(_db.CreateParameter("@ThumbMediaId", (object?)thumbId ?? DBNull.Value));
                            cm.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                            await cm.ExecuteNonQueryAsync();
                            break;
                        }

                    case 2: // Ticket
                        {
                            await using var ct = _db.CreateCommand(conn, ItemSql.InsertTicket, tx);
                            ct.Parameters.Add(_db.CreateParameter("@ItemId", newItemId));
                            ct.Parameters.Add(_db.CreateParameter("@EventId", dto.Ticket!.EventId));
                            ct.Parameters.Add(_db.CreateParameter("@TicketTypeId", dto.Ticket.TicketTypeId));
                            ct.Parameters.Add(_db.CreateParameter("@Seat", (object?)dto.Ticket.Seat ?? DBNull.Value));
                            ct.Parameters.Add(_db.CreateParameter("@Section", (object?)dto.Ticket.Section ?? DBNull.Value));
                            ct.Parameters.Add(_db.CreateParameter("@ValidFrom", (object?)dto.Ticket.ValidFrom ?? DBNull.Value));
                            ct.Parameters.Add(_db.CreateParameter("@ValidTo", (object?)dto.Ticket.ValidTo ?? DBNull.Value));
                            ct.Parameters.Add(_db.CreateParameter("@IsTransferable", dto.Ticket.IsTransferable ? 1 : 0));
                            ct.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                            await ct.ExecuteNonQueryAsync();
                            break;
                        }

                    case 3: // Badge
                        {
                            // Resolve/create media (unchanged)
                            string? mediaId = dto.Badge?.MediaId;
                            if (string.IsNullOrWhiteSpace(mediaId) && dto.Badge?.Media is not null)
                            {
                                mediaId = await FilterAndInsertMediaAsync(
                                    conn, tx, spaceId, supportedLocales, dto.Badge.Media, modifiedBy);
                            }

                            // Prefer keys provided inside LocalizedPairs; otherwise fall back to the item’s keys
                            var badgeNameKey = dto.Badge?.NameLocalizedPairs?.Key ?? dto.NameKey;
                            var badgeDescKey = dto.Badge?.DescriptionLocalizedPairs?.Key ?? dto.DescriptionKey;

                            // Upsert i18n for badge name (only if badge provided its own values)
                            if (!string.IsNullOrWhiteSpace(badgeNameKey) &&
                                dto.Badge?.NameLocalizedPairs?.Values is { Count: > 0 } nameVals)
                            {
                                var filtered = nameVals
                                    .Where(v => !string.IsNullOrWhiteSpace(v.LocaleId) &&
                                                supportedLocales.Contains(v.LocaleId))
                                    .ToList();
                                if (filtered.Count > 0)
                                    await UpsertI18nAsync(spaceId, badgeNameKey!, filtered, modifiedBy, conn, tx);
                            }

                            // Upsert i18n for badge description (only if badge provided its own values)
                            if (!string.IsNullOrWhiteSpace(badgeDescKey) &&
                                dto.Badge?.DescriptionLocalizedPairs?.Values is { Count: > 0 } descVals)
                            {
                                var filtered = descVals
                                    .Where(v => !string.IsNullOrWhiteSpace(v.LocaleId) &&
                                                supportedLocales.Contains(v.LocaleId))
                                    .ToList();
                                if (filtered.Count > 0)
                                    await UpsertI18nAsync(spaceId, badgeDescKey!, filtered, modifiedBy, conn, tx);
                            }

                            // Insert into badge table using the resolved keys
                            await using var cb = _db.CreateCommand(conn, ItemSql.InsertBadge, tx);
                            cb.Parameters.Add(_db.CreateParameter("@ItemId", newItemId));
                            cb.Parameters.Add(_db.CreateParameter("@NameKey", (object?)badgeNameKey ?? DBNull.Value));
                            cb.Parameters.Add(_db.CreateParameter("@DescriptionKey", (object?)badgeDescKey ?? DBNull.Value));
                            cb.Parameters.Add(_db.CreateParameter("@MediaId", (object?)mediaId ?? DBNull.Value));
                            cb.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                            await cb.ExecuteNonQueryAsync();
                            break;
                        }


                    default:
                        throw new InvalidOperationException($"Unsupported ItemTypeId: {dto.ItemTypeId}");
                }

                // D) optional one-price insert
                if (dto.RegionalCurrencyId is int rcId && dto.BaseCost is long baseCost)
                {
                    await using var ip = _db.CreateCommand(conn, ItemSql.InsertItemPrice, tx);
                    ip.Parameters.Add(_db.CreateParameter("@ItemId", newItemId));
                    ip.Parameters.Add(_db.CreateParameter("@RegionalCurrencyId", rcId));
                    ip.Parameters.Add(_db.CreateParameter("@BaseCost", baseCost));
                    ip.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                    await ip.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                // E) read minimal envelope
                await using var sel = _db.CreateCommand(conn, ItemSql.SelectCreatedItemEnvelope);
                sel.Parameters.Add(_db.CreateParameter("@ItemId", newItemId));
                sel.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                await using var rdr = await sel.ExecuteReaderAsync();
                if (!await rdr.ReadAsync()) throw new InvalidOperationException("Created item not found.");

                return new ItemCreatedEnvelope
                {
                    ItemId = rdr.GetInt32(rdr.GetOrdinal("item_id")),
                    ItemTypeId = rdr.GetInt32(rdr.GetOrdinal("item_type_id")),
                    NameKey = rdr.GetString(rdr.GetOrdinal("name_key")),
                    DescriptionKey = rdr.IsDBNull(rdr.GetOrdinal("description_key")) ? null : rdr.GetString(rdr.GetOrdinal("description_key")),
                    IsAvailable = rdr.GetInt32(rdr.GetOrdinal("is_available")) == 1
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task<int> UpsertI18nAsync(
    int spaceId,
    string key,
    IEnumerable<LocalizedValue> pairs,
    string modifiedBy,
    DbConnection conn,
    DbTransaction tx)
        {
            var total = 0;

            foreach (var p in pairs)
            {
                // UPDATE
                await using (var up = _db.CreateCommand(conn, ShopItemsSql.UpdateI18nValue, tx))
                {
                    up.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    up.Parameters.Add(_db.CreateParameter("@Key", key));
                    up.Parameters.Add(_db.CreateParameter("@LocaleId", p.LocaleId));
                    up.Parameters.Add(_db.CreateParameter("@Value", p.Value));
                    up.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));   // <-- IMPORTANT
                    var affected = await up.ExecuteNonQueryAsync();
                    total += affected;

                    if (affected == 0)
                    {
                        // INSERT
                        await using var ins = _db.CreateCommand(conn, ShopItemsSql.InsertI18nValue, tx);
                        ins.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        ins.Parameters.Add(_db.CreateParameter("@Key", key));
                        ins.Parameters.Add(_db.CreateParameter("@LocaleId", p.LocaleId));
                        ins.Parameters.Add(_db.CreateParameter("@Value", p.Value));
                        ins.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy)); // <-- IMPORTANT
                        total += await ins.ExecuteNonQueryAsync();
                    }
                }
            }

            return total;
        }



        private async Task<HashSet<string>> GetSupportedLocalesInternalAsync(DbConnection conn, DbTransaction tx, int spaceId)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var cmd = _db.CreateCommand(conn, ItemSql.GetSupportedLocales, tx);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync()) set.Add(rdr.GetString(0));
            return set;
        }

        private async Task<string?> FilterAndInsertMediaAsync(
            DbConnection conn, DbTransaction tx, int spaceId, HashSet<string> supportedLocales,
            MediaCreateDto mediaDto, string modifiedBy)
        {
            // Filter localizations to supported locales
            if (mediaDto.LinkLocalizations is not null)
                mediaDto.LinkLocalizations = mediaDto.LinkLocalizations
                    .Where(x => supportedLocales.Contains(x.LocaleId))
                    .ToList();

            if (mediaDto.TextLocalizations is not null)
                mediaDto.TextLocalizations = mediaDto.TextLocalizations
                    .Where(x => supportedLocales.Contains(x.LocaleId))
                    .ToList();

            if (mediaDto.DescriptionLocalizations is not null)
                mediaDto.DescriptionLocalizations = mediaDto.DescriptionLocalizations
                    .Where(x => supportedLocales.Contains(x.LocaleId))
                    .ToList();

            // Must have at least one link to create the media
            if (mediaDto.LinkLocalizations is null || mediaDto.LinkLocalizations.Count == 0)
                return null;

            return await InsertMediaAsync(conn, tx, spaceId, mediaDto, modifiedBy);
        }

        private async Task<string> InsertMediaAsync(
            DbConnection conn, DbTransaction tx, int spaceId, MediaCreateDto dto, string modifiedBy)
        {
            // 1) get UUID
            string mediaId;
            await using (var uuidCmd = _db.CreateCommand(conn, ItemSql.NewUuid, tx))
                mediaId = (await uuidCmd.ExecuteScalarAsync())!.ToString()!;

            // 2) media row
            await using (var cmd = _db.CreateCommand(conn, ItemSql.InsertMedia, tx))
            {
                cmd.Parameters.Add(_db.CreateParameter("@Id", mediaId));
                cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                cmd.Parameters.Add(_db.CreateParameter("@MediaTypeId", dto.MediaTypeId));
                cmd.Parameters.Add(_db.CreateParameter("@TextKey", (object?)dto.TextKey ?? DBNull.Value));
                cmd.Parameters.Add(_db.CreateParameter("@DescKey", (object?)dto.DescriptionKey ?? DBNull.Value));
                cmd.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                await cmd.ExecuteNonQueryAsync();
            }

            // 3) i18n text values
            if (dto.TextKey != null && dto.TextLocalizations is not null)
            {
                foreach (var loc in dto.TextLocalizations)
                {
                    await using var cmdI = _db.CreateCommand(conn, ItemSql.InsertI18nSimple, tx);
                    cmdI.Parameters.Add(_db.CreateParameter("@Key", dto.TextKey));
                    cmdI.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                    cmdI.Parameters.Add(_db.CreateParameter("@Value", loc.Value));
                    cmdI.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmdI.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                    await cmdI.ExecuteNonQueryAsync();
                }
            }

            // 4) i18n description values
            if (dto.DescriptionKey != null && dto.DescriptionLocalizations is not null)
            {
                foreach (var loc in dto.DescriptionLocalizations)
                {
                    await using var cmdI = _db.CreateCommand(conn, ItemSql.InsertI18nSimple, tx);
                    cmdI.Parameters.Add(_db.CreateParameter("@Key", dto.DescriptionKey));
                    cmdI.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                    cmdI.Parameters.Add(_db.CreateParameter("@Value", loc.Value));
                    cmdI.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    cmdI.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                    await cmdI.ExecuteNonQueryAsync();
                }
            }

            // 5) media links
            if (dto.LinkLocalizations is not null)
            {
                foreach (var loc in dto.LinkLocalizations)
                {
                    await using var cmdL = _db.CreateCommand(conn, ItemSql.InsertMediaLocalization, tx);
                    cmdL.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                    cmdL.Parameters.Add(_db.CreateParameter("@LocaleId", loc.LocaleId));
                    cmdL.Parameters.Add(_db.CreateParameter("@MediaLink", loc.MediaLink));
                    await cmdL.ExecuteNonQueryAsync();
                }
            }

            return mediaId;
        }

        public async Task<IReadOnlyList<ShopItemsListRow>> ListAllBySpaceAsync(int spaceId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ItemSql.ListAllBySpace);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            var map = new Dictionary<(int ItemId, int? CurrencyId), ShopItemsListRow>();
            await using var rdr = await cmd.ExecuteReaderAsync();

            while (await rdr.ReadAsync())
            {
                var itemId = rdr.GetInt32(rdr.GetOrdinal("item_id"));
                var itemTypeId = rdr.GetInt32(rdr.GetOrdinal("item_type_id"));
                var currencyId = rdr.IsDBNull(rdr.GetOrdinal("currency_id")) ? (int?)null : rdr.GetInt32(rdr.GetOrdinal("currency_id"));
                var key = (itemId, currencyId);

                if (!map.TryGetValue(key, out var row))
                {
                    var nameKey = rdr.GetString(rdr.GetOrdinal("name_key"));
                    var isAvailable = rdr.GetInt32(rdr.GetOrdinal("is_available")) == 1;
                    var priceMinor = rdr.IsDBNull(rdr.GetOrdinal("price_minor_units")) ? 0L : rdr.GetInt64(rdr.GetOrdinal("price_minor_units"));
                    var descKey = rdr.IsDBNull(rdr.GetOrdinal("description_key")) ? null : rdr.GetString(rdr.GetOrdinal("description_key"));

                    ShopItemsDiscount? discount = null;
                    if (!rdr.IsDBNull(rdr.GetOrdinal("discount_name_key")))
                    {
                        discount = new ShopItemsDiscount
                        {
                            Name_Key = rdr.GetString(rdr.GetOrdinal("discount_name_key")),
                            Discount_Type = rdr.GetInt32(rdr.GetOrdinal("discount_type_id")),
                            Value_Minor_Units = rdr.GetInt64(rdr.GetOrdinal("discount_value_minor_units")),
                            Start_At = rdr.IsDBNull(rdr.GetOrdinal("discount_start_at")) ? default : rdr.GetDateTime(rdr.GetOrdinal("discount_start_at")),
                            End_At = rdr.IsDBNull(rdr.GetOrdinal("discount_end_at")) ? default : rdr.GetDateTime(rdr.GetOrdinal("discount_end_at")),
                        };
                    }

                    row = new ShopItemsListRow
                    {
                        Item = new ShopItemsItem
                        {
                            Id = itemId,
                            Item_Type_Id = itemTypeId,
                            Name_Key = nameKey,
                            Description_Key = descKey,
                            Is_Available = isAvailable
                        },
                        Shop_Item = new ShopItemsShopPart
                        {
                            Price_Minor_Units = priceMinor,       // base_cost from item_prices
                            Stock_Quantity = 0,                // not tracked at item level
                            Currency_Id = currencyId ?? 0,  // 0 when price missing
                            Discount = discount
                        }
                    };

                    // subtype shells (minimal)
                    switch (itemTypeId)
                    {
                        case 1: // merchandise
                            if (!rdr.IsDBNull(rdr.GetOrdinal("merch_type_id")))
                            {
                                row.Merchandise = new MerchandiseRead
                                {
                                    Merchandise_Type_Id = rdr.GetInt32(rdr.GetOrdinal("merch_type_id")),
                                    Merchandise_Category_Id = rdr.IsDBNull(rdr.GetOrdinal("merch_category_id")) ? (int?)null : rdr.GetInt32(rdr.GetOrdinal("merch_category_id")),
                                    Asset_Url = rdr.IsDBNull(rdr.GetOrdinal("merch_asset_url")) ? null : rdr.GetString(rdr.GetOrdinal("merch_asset_url")),
                                    Thumbnail = null
                                };
                            }
                            break;

                        case 2: // ticket
                            if (!rdr.IsDBNull(rdr.GetOrdinal("ticket_type_id")))
                            {
                                row.Ticket = new TicketRead
                                {
                                    Event_Id = rdr.GetInt32(rdr.GetOrdinal("ticket_event_id")),
                                    Ticket_Type_Id = rdr.GetInt32(rdr.GetOrdinal("ticket_type_id")),
                                    Seat = rdr.IsDBNull(rdr.GetOrdinal("ticket_seat")) ? null : rdr.GetString(rdr.GetOrdinal("ticket_seat")),
                                    Section = rdr.IsDBNull(rdr.GetOrdinal("ticket_section")) ? null : rdr.GetString(rdr.GetOrdinal("ticket_section")),
                                    Valid_From = rdr.IsDBNull(rdr.GetOrdinal("ticket_valid_from")) ? (DateTime?)null : rdr.GetDateTime(rdr.GetOrdinal("ticket_valid_from")),
                                    Valid_To = rdr.IsDBNull(rdr.GetOrdinal("ticket_valid_to")) ? (DateTime?)null : rdr.GetDateTime(rdr.GetOrdinal("ticket_valid_to")),
                                    Is_Transferable = !rdr.IsDBNull(rdr.GetOrdinal("ticket_is_transferable")) && rdr.GetInt32(rdr.GetOrdinal("ticket_is_transferable")) == 1
                                };
                            }
                            break;

                        case 3: // badge
                            if (!rdr.IsDBNull(rdr.GetOrdinal("badge_media_id")))
                            {
                                row.Badge = new BadgeRead
                                {
                                    Media = new Models.MediaData
                                    {
                                        Id = rdr.GetInt32(rdr.GetOrdinal("badge_media_id")),
                                        MediaTypeId = 0,
                                        TextKey = null,
                                        DescriptionKey = null,
                                        LinkLocalizations = new List<Models.MediaLocalization>()
                                    }
                                };
                            }
                            break;
                    }

                    map[key] = row;
                }

                // per-locale stitching for media links
                var locIdx = rdr.GetOrdinal("locale_id");
                if (!rdr.IsDBNull(locIdx))
                {
                    var locale = rdr.GetString(locIdx);

                    if (row.Merchandise != null && !rdr.IsDBNull(rdr.GetOrdinal("merch_thumb_media_id")))
                    {
                        row.Merchandise.Thumbnail ??= new Models.MediaData
                        {
                            Id = rdr.GetInt32(rdr.GetOrdinal("merch_thumb_media_id")),
                            MediaTypeId = 0,
                            TextKey = null,
                            DescriptionKey = null,
                            LinkLocalizations = new List<Models.MediaLocalization>()
                        };

                        var link = rdr.IsDBNull(rdr.GetOrdinal("merch_thumb_media_link"))
                            ? null : rdr.GetString(rdr.GetOrdinal("merch_thumb_media_link"));

                        if (link != null && !row.Merchandise.Thumbnail.LinkLocalizations.Any(x => x.LocaleId == locale))
                        {
                            row.Merchandise.Thumbnail.LinkLocalizations.Add(new Models.MediaLocalization
                            {
                                LocaleId = locale,
                                MediaLink = link
                            });
                        }
                    }

                    if (row.Badge?.Media != null && row.Badge.Media.Id == 0)
                    {
                        var link = rdr.IsDBNull(rdr.GetOrdinal("badge_media_link"))
                            ? null : rdr.GetString(rdr.GetOrdinal("badge_media_link"));

                        if (link != null && !row.Badge.Media.LinkLocalizations.Any(x => x.LocaleId == locale))
                        {
                            row.Badge.Media.LinkLocalizations.Add(new Models.MediaLocalization
                            {
                                LocaleId = locale,
                                MediaLink = link
                            });
                        }
                    }
                }
            }

            return map.Values.ToList();
        }


        public async Task UpdateAsync(int spaceId, int itemId, UpdateItemDto dto, string modifiedBy)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 0) validate item in space & get snapshot (keys)
                string nameKey;
                string? descKey;
                await using (var chk = _db.CreateCommand(conn, ItemSql.ExistsItemInSpace, tx))
                {
                    chk.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
                    chk.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    if (await chk.ExecuteScalarAsync() is null)
                        throw new InvalidOperationException("Item not found in space.");
                }
                await using (var snap = _db.CreateCommand(conn, ItemSql.GetItemSnapshot, tx))
                {
                    snap.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
                    await using var rdr = await snap.ExecuteReaderAsync();
                    if (!await rdr.ReadAsync())
                        throw new InvalidOperationException("Item not found.");
                    nameKey = rdr.GetString(rdr.GetOrdinal("NameKey"));
                    descKey = rdr.IsDBNull(rdr.GetOrdinal("DescriptionKey")) ? null : rdr.GetString(rdr.GetOrdinal("DescriptionKey"));
                }

                // 1) basics
                if (dto.IsAvailable.HasValue)
                {
                    await using var ub = _db.CreateCommand(conn, ItemSql.UpdateItemBasics, tx);
                    ub.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
                    ub.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    ub.Parameters.Add(_db.CreateParameter("@IsAvailable", dto.IsAvailable.Value ? 1 : 0));
                    ub.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                    await ub.ExecuteNonQueryAsync();
                }

                // 2) i18n (optional)
                if (dto.NameLocalizedPairs?.Values?.Count > 0)
                    await UpsertI18nInternalAsync( spaceId, nameKey, dto.NameLocalizedPairs.Values, modifiedBy);

                if (!string.IsNullOrWhiteSpace(descKey) && dto.DescriptionLocalizedPairs?.Values?.Count > 0)
                    await UpsertI18nInternalAsync( spaceId, descKey!, dto.DescriptionLocalizedPairs.Values, modifiedBy);

                // 3) prices (upsert)
                if (dto.Prices is { Count: > 0 })
                {
                    foreach (var p in dto.Prices)
                    {
                        await using var up = _db.CreateCommand(conn, ItemSql.UpsertItemPrice, tx);
                        up.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
                        up.Parameters.Add(_db.CreateParameter("@RegionalCurrencyId", p.RegionalCurrencyId));
                        up.Parameters.Add(_db.CreateParameter("@BaseCost", p.BaseCost));
                        up.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                        await up.ExecuteNonQueryAsync();
                    }
                }

                // 4) subtype patches
                // Merchandise
                if (dto.Merchandise is not null)
                {
                    // media handling: optional create & set, or clear, or no change
                    string? thumbIdToSet;
                    if (dto.Merchandise.ClearThumbnail == true)
                        thumbIdToSet = null;  // explicit NULL
                    else if (dto.Merchandise.ThumbnailMedia is not null)
                        thumbIdToSet = await InsertMediaIfAnyAsync(_db, conn, tx, spaceId, dto.Merchandise.ThumbnailMedia, modifiedBy);
                    else
                        thumbIdToSet = dto.Merchandise.ThumbnailMediaId; // may be null (no change if null & no clear flag)

                    // If no explicit change requested (no clear, no id, no create), keep current value
                    var keepCurrentThumb = (dto.Merchandise.ClearThumbnail != true
                                            && dto.Merchandise.ThumbnailMedia is null
                                            && dto.Merchandise.ThumbnailMediaId is null);

                    // Bind update
                    await using var um = _db.CreateCommand(conn, ItemSql.UpdateMerchandise, tx);
                    um.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
                    um.Parameters.Add(_db.CreateParameter("@MerchTypeId", (object?)dto.Merchandise.MerchandiseTypeId ?? DBNull.Value));
                    um.Parameters.Add(_db.CreateParameter("@MerchCategoryId", (object?)dto.Merchandise.MerchandiseCategoryId ?? DBNull.Value));
                    um.Parameters.Add(_db.CreateParameter("@AssetUrl", (object?)dto.Merchandise.AssetUrl ?? DBNull.Value));
                    // special: thumbnail_media_id
                    if (keepCurrentThumb)
                    {
                        // read current value to keep (so the query can set it to itself)
                        string? currentThumb = await GetCurrentMerchThumbAsync(conn, tx, itemId);
                        um.Parameters.Add(_db.CreateParameter("@ThumbMediaId_Value", (object?)currentThumb ?? DBNull.Value));
                    }
                    else
                    {
                        um.Parameters.Add(_db.CreateParameter("@ThumbMediaId_Value", (object?)thumbIdToSet ?? DBNull.Value));
                    }
                    um.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                    await um.ExecuteNonQueryAsync();
                }

                // Ticket
                if (dto.Ticket is not null)
                {
                    await using var ut = _db.CreateCommand(conn, ItemSql.UpdateTicket, tx);
                    ut.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
                    ut.Parameters.Add(_db.CreateParameter("@EventId", (object?)dto.Ticket.EventId ?? DBNull.Value));
                    ut.Parameters.Add(_db.CreateParameter("@TicketTypeId", (object?)dto.Ticket.TicketTypeId ?? DBNull.Value));
                    ut.Parameters.Add(_db.CreateParameter("@Seat", (object?)dto.Ticket.Seat ?? DBNull.Value));
                    ut.Parameters.Add(_db.CreateParameter("@Section", (object?)dto.Ticket.Section ?? DBNull.Value));
                    ut.Parameters.Add(_db.CreateParameter("@ValidFrom", (object?)dto.Ticket.ValidFrom ?? DBNull.Value));
                    ut.Parameters.Add(_db.CreateParameter("@ValidTo", (object?)dto.Ticket.ValidTo ?? DBNull.Value));
                    ut.Parameters.Add(_db.CreateParameter("@IsTransferable", dto.Ticket.IsTransferable.HasValue ? (dto.Ticket.IsTransferable.Value ? 1 : 0) : (object)DBNull.Value));
                    ut.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                    await ut.ExecuteNonQueryAsync();
                }

                // Badge
                // Badge
                if (dto.Badge is not null)
                {
                    // 1) Read current badge state so we can "keep current" when fields are not provided
                    var (currentMediaId, currentBadgeNameKey, currentBadgeDescKey) =
                        await GetCurrentBadgeAsync(conn, tx, itemId); // implement to SELECT media_id,name_key,description_key FROM badge WHERE id=@ItemId

                    // 2) Media: clear / create-new / set-id / keep current
                    string? mediaIdToSet;
                    if (dto.Badge.ClearMedia == true)
                        mediaIdToSet = null;
                    else if (dto.Badge.Media is not null)
                        mediaIdToSet = await InsertMediaIfAnyAsync(_db, conn, tx, spaceId, dto.Badge.Media, modifiedBy);
                    else
                        mediaIdToSet = dto.Badge.MediaId ?? currentMediaId;

                    // 3) Keys to use for name/description (prefer keys provided inside LocalizedPairs)
                    var desiredNameKey = dto.Badge.NameLocalizedPairs?.Key ?? currentBadgeNameKey ?? nameKey;      // fallback to item nameKey
                    var desiredDescKey = dto.Badge.DescriptionLocalizedPairs?.Key ?? currentBadgeDescKey ?? descKey;

                    // 4) Upsert i18n if new values were provided for badge name/description
                    if (dto.Badge.NameLocalizedPairs?.Values is { Count: > 0 } nameVals)
                        await UpsertI18nInternalAsync(spaceId, desiredNameKey!, nameVals, modifiedBy);

                    if (!string.IsNullOrWhiteSpace(desiredDescKey) &&
                        dto.Badge.DescriptionLocalizedPairs?.Values is { Count: > 0 } descVals)
                        await UpsertI18nInternalAsync(spaceId, desiredDescKey!, descVals, modifiedBy);

                    // 5) Persist to badge table (media_id/name_key/description_key)
                    await using var ub = _db.CreateCommand(conn, ItemSql.UpdateBadge, tx);
                    ub.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
                    ub.Parameters.Add(_db.CreateParameter("@MediaId_Value", (object?)mediaIdToSet ?? DBNull.Value));
                    ub.Parameters.Add(_db.CreateParameter("@NameKey_Value", (object?)desiredNameKey ?? DBNull.Value));
                    ub.Parameters.Add(_db.CreateParameter("@DescKey_Value", (object?)desiredDescKey ?? DBNull.Value));
                    ub.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                    await ub.ExecuteNonQueryAsync();
                }


                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task<int> UpsertI18nInternalAsync(int spaceId, string key, IEnumerable<LocalizedValue> pairs, string modifiedBy)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                var total = 0;
                foreach (var p in pairs)
                {
                    await using var up = _db.CreateCommand(conn, ShopItemsSql.UpdateI18nValue, tx);
                    up.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    up.Parameters.Add(_db.CreateParameter("@Key", key));
                    up.Parameters.Add(_db.CreateParameter("@LocaleId", p.LocaleId));
                    up.Parameters.Add(_db.CreateParameter("@Value", p.Value));
                    up.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                    var affected = await up.ExecuteNonQueryAsync();
                    total += affected;

                    if (affected == 0)
                    {
                        await using var ins = _db.CreateCommand(conn, ShopItemsSql.InsertI18nValue, tx);
                        ins.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                        ins.Parameters.Add(_db.CreateParameter("@Key", key));
                        ins.Parameters.Add(_db.CreateParameter("@LocaleId", p.LocaleId));
                        ins.Parameters.Add(_db.CreateParameter("@Value", p.Value));
                        ins.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                        total += await ins.ExecuteNonQueryAsync();
                    }
                }
                await tx.CommitAsync();
                return total;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task<string?> GetCurrentMerchThumbAsync(DbConnection conn, DbTransaction tx, int itemId)
        {
            const string sql = @"SELECT thumbnail_media_id FROM merchandise WHERE id=@ItemId;";
            await using var cmd = _db.CreateCommand(conn, sql, tx);
            cmd.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
            var o = await cmd.ExecuteScalarAsync();
            return o == DBNull.Value ? null : (o?.ToString());
        }

        private async Task<string?> GetCurrentBadgeMediaAsync(DbConnection conn, DbTransaction tx, int itemId)
        {
            const string sql = @"SELECT media_id FROM badge WHERE id=@ItemId;";
            await using var cmd = _db.CreateCommand(conn, sql, tx);
            cmd.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
            var o = await cmd.ExecuteScalarAsync();
            return o == DBNull.Value ? null : (o?.ToString());
        }

        private async Task<(string? mediaId, string? nameKey, string? descKey)>
    GetCurrentBadgeAsync(DbConnection conn, DbTransaction tx, int itemId)
        {
            const string sql = @"
        SELECT media_id, name_key, description_key
        FROM badge
        WHERE id = @ItemId
        LIMIT 1;"; // use TOP 1 for SQL Server

            await using var cmd = _db.CreateCommand(conn, sql, tx);
            cmd.Parameters.Add(_db.CreateParameter("@ItemId", itemId));

            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync())
                return (null, null, null);

            string? mediaId = rdr.IsDBNull(0) ? null : rdr.GetString(0);
            string? nameKey = rdr.IsDBNull(1) ? null : rdr.GetString(1);
            string? descKey = rdr.IsDBNull(2) ? null : rdr.GetString(2);
            return (mediaId, nameKey, descKey);
        }


        /// <summary>
        /// Inserts media + i18n + media_localization. Returns the new media id.
        /// Assumes MySQL (UUID()) and uses your IDbProvider wrapper.
        /// </summary>
        internal static async Task<string> InsertMediaIfAnyAsync(
            IDbProvider db,
            DbConnection conn,
            DbTransaction tx,
            int spaceId,
            MediaCreateDto dto,
            string modifiedBy // currently unused but kept for parity
        )
        {
            // 1) generate id
            string mediaId;
            await using (var uuidCmd = db.CreateCommand(conn, "SELECT UUID();", tx))
            {
                mediaId = (await uuidCmd.ExecuteScalarAsync())!.ToString()!;
            }

            // 2) insert media row
            const string insMedia = @"
INSERT INTO media (id, space_id, media_type_id, text_key, description_key)
VALUES (@Id, @SpaceId, @MediaTypeId, @TextKey, @DescKey);";

            await using (var cmd = db.CreateCommand(conn, insMedia, tx))
            {
                cmd.Parameters.Add(db.CreateParameter("@Id", mediaId));
                cmd.Parameters.Add(db.CreateParameter("@SpaceId", spaceId));
                cmd.Parameters.Add(db.CreateParameter("@MediaTypeId", dto.MediaTypeId));
                cmd.Parameters.Add(db.CreateParameter("@TextKey", (object?)dto.TextKey ?? DBNull.Value));
                cmd.Parameters.Add(db.CreateParameter("@DescKey", (object?)dto.DescriptionKey ?? DBNull.Value));
                await cmd.ExecuteNonQueryAsync();
            }

            // 3) i18n: text
            if (!string.IsNullOrWhiteSpace(dto.TextKey) && dto.TextLocalizations is { Count: > 0 })
            {
                const string insI18n = @"INSERT INTO i18n (`key`, locale_id, value, space_id)
                                         VALUES (@Key, @LocaleId, @Value, @SpaceId);";
                foreach (var loc in dto.TextLocalizations)
                {
                    await using var cmdI = db.CreateCommand(conn, insI18n, tx);
                    cmdI.Parameters.Add(db.CreateParameter("@Key", dto.TextKey!));
                    cmdI.Parameters.Add(db.CreateParameter("@LocaleId", loc.LocaleId));
                    cmdI.Parameters.Add(db.CreateParameter("@Value", loc.Value));
                    cmdI.Parameters.Add(db.CreateParameter("@SpaceId", spaceId));
                    await cmdI.ExecuteNonQueryAsync();
                }
            }

            // 4) i18n: description
            if (!string.IsNullOrWhiteSpace(dto.DescriptionKey) && dto.DescriptionLocalizations is { Count: > 0 })
            {
                const string insI18n = @"INSERT INTO i18n (`key`, locale_id, value, space_id)
                                         VALUES (@Key, @LocaleId, @Value, @SpaceId);";
                foreach (var loc in dto.DescriptionLocalizations)
                {
                    await using var cmdI = db.CreateCommand(conn, insI18n, tx);
                    cmdI.Parameters.Add(db.CreateParameter("@Key", dto.DescriptionKey!));
                    cmdI.Parameters.Add(db.CreateParameter("@LocaleId", loc.LocaleId));
                    cmdI.Parameters.Add(db.CreateParameter("@Value", loc.Value));
                    cmdI.Parameters.Add(db.CreateParameter("@SpaceId", spaceId));
                    await cmdI.ExecuteNonQueryAsync();
                }
            }

            // 5) media_localization links
            if (dto.LinkLocalizations is { Count: > 0 })
            {
                const string insLoc = @"
INSERT INTO media_localization (media_id, locale_id, media_link)
VALUES (@MediaId, @LocaleId, @MediaLink);";
                foreach (var loc in dto.LinkLocalizations)
                {
                    await using var cmdLoc = db.CreateCommand(conn, insLoc, tx);
                    cmdLoc.Parameters.Add(db.CreateParameter("@MediaId", mediaId));
                    cmdLoc.Parameters.Add(db.CreateParameter("@LocaleId", loc.LocaleId));
                    cmdLoc.Parameters.Add(db.CreateParameter("@MediaLink", loc.MediaLink));
                    await cmdLoc.ExecuteNonQueryAsync();
                }
            }

            return mediaId;
        }


        public async Task DeleteAsync(int spaceId, int itemId, string modifiedBy)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Load item + subtype media references + i18n keys
                int itemTypeId;
                string nameKey;
                string? descKey = null;
                string? merchThumbId = null;
                string? badgeMediaId = null;

                await using (var cmd = _db.CreateCommand(conn, ItemSql.GetItemDeleteInfo, tx))
                {
                    cmd.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
                    cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                    await using var rdr = await cmd.ExecuteReaderAsync();
                    if (!await rdr.ReadAsync())
                        throw new InvalidOperationException("Item not found in this space.");

                    itemTypeId = rdr.GetInt32(rdr.GetOrdinal("item_type_id"));
                    nameKey = rdr.GetString(rdr.GetOrdinal("name_key"));
                    descKey = rdr.IsDBNull(rdr.GetOrdinal("description_key"))
                                    ? null : rdr.GetString(rdr.GetOrdinal("description_key"));
                    merchThumbId = rdr.IsDBNull(rdr.GetOrdinal("merch_thumb_media_id"))
                                    ? null : rdr.GetString(rdr.GetOrdinal("merch_thumb_media_id"));
                    badgeMediaId = rdr.IsDBNull(rdr.GetOrdinal("badge_media_id"))
                                    ? null : rdr.GetString(rdr.GetOrdinal("badge_media_id"));
                }

                // 2) Remove mappings & prices/discounts first
                await ExecAsync(conn, tx, ItemSql.DeleteShopItems, ("@ItemId", itemId));
                await ExecAsync(conn, tx, ItemSql.DeleteItemDiscounts, ("@ItemId", itemId));
                await ExecAsync(conn, tx, ItemSql.DeleteItemPrices, ("@ItemId", itemId));

                // 3) Delete subtype row
                switch (itemTypeId)
                {
                    case 1: await ExecAsync(conn, tx, ItemSql.DeleteMerchandise, ("@ItemId", itemId)); break;
                    case 2: await ExecAsync(conn, tx, ItemSql.DeleteTicket, ("@ItemId", itemId)); break;
                    case 3: await ExecAsync(conn, tx, ItemSql.DeleteBadge, ("@ItemId", itemId)); break;
                }

                // 4) Delete referenced media (gather their i18n keys, delete localizations + media + media i18n)
                if (!string.IsNullOrWhiteSpace(merchThumbId))
                    await DeleteMediaCascadeAsync(conn, tx, spaceId, merchThumbId!);

                if (!string.IsNullOrWhiteSpace(badgeMediaId))
                    await DeleteMediaCascadeAsync(conn, tx, spaceId, badgeMediaId!);

                // 5) Delete item (enforces space ownership)
                await ExecAsync(conn, tx, ItemSql.DeleteItem, ("@ItemId", itemId), ("@SpaceId", spaceId));

                // 6) Delete item name/description i18n
                await ExecAsync(conn, tx, ItemSql.DeleteI18nByKey, ("@Key", nameKey), ("@SpaceId", spaceId));
                if (!string.IsNullOrWhiteSpace(descKey))
                    await ExecAsync(conn, tx, ItemSql.DeleteI18nByKey, ("@Key", descKey!), ("@SpaceId", spaceId));

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task DeleteMediaCascadeAsync(DbConnection conn, DbTransaction tx, int spaceId, string mediaId)
        {
            // Get media keys (if any) before deletion
            string? textKey = null, descriptionKey = null;

            await using (var g = _db.CreateCommand(conn, ItemSql.GetMediaKeys, tx))
            {
                g.Parameters.Add(_db.CreateParameter("@MediaId", mediaId));
                g.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

                await using var rdr = await g.ExecuteReaderAsync();
                if (await rdr.ReadAsync())
                {
                    textKey = rdr.IsDBNull(rdr.GetOrdinal("text_key")) ? null : rdr.GetString(rdr.GetOrdinal("text_key"));
                    descriptionKey = rdr.IsDBNull(rdr.GetOrdinal("description_key")) ? null : rdr.GetString(rdr.GetOrdinal("description_key"));
                }
            }

            // Delete media_localization → media
            await ExecAsync(conn, tx, ItemSql.DeleteMediaLocalization, ("@MediaId", mediaId));
            await ExecAsync(conn, tx, ItemSql.DeleteMedia, ("@MediaId", mediaId), ("@SpaceId", spaceId));

            // Delete i18n for media text/description keys (if present)
            if (!string.IsNullOrWhiteSpace(textKey))
                await ExecAsync(conn, tx, ItemSql.DeleteI18nByKey, ("@Key", textKey!), ("@SpaceId", spaceId));
            if (!string.IsNullOrWhiteSpace(descriptionKey))
                await ExecAsync(conn, tx, ItemSql.DeleteI18nByKey, ("@Key", descriptionKey!), ("@SpaceId", spaceId));
        }

        private async Task ExecAsync(DbConnection conn, DbTransaction tx, string sql, params (string name, object value)[] ps)
        {
            await using var cmd = _db.CreateCommand(conn, sql, tx);
            foreach (var (name, value) in ps)
                cmd.Parameters.Add(_db.CreateParameter(name, value));
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
