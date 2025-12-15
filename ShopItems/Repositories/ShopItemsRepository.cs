// <copyright file="ShopItemsRepository.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>11/25/2025</date>
// <summary>Repository implementation for shop-item listing.</summary>

using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Models.Common;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using GMS.TifoXRCoreWebAPI.Repositories.Sql;
using GMS.TifoXRCoreWebAPI.Utilities.Infrastructure;
using System.Data.Common;

namespace GMS.TifoXRCoreWebAPI.Repositories
{
    public sealed class ShopItemsRepository : IShopItemsRepository
    {
        private readonly IDbProvider _db;
        public ShopItemsRepository(IDbProvider db) => _db = db;

        public async Task<bool> ShopExistsInSpaceAsync(int spaceId, int shopId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopItemsSql.ExistsShopInSpace);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
            var obj = await cmd.ExecuteScalarAsync();
            return obj is not null;
        }

        public async Task<IReadOnlyList<ShopItemsListRow>> ListByShopAsync(int spaceId, int shopId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopItemsSql.ListItemsByShop);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@ShopId", shopId));

            var map = new Dictionary<int, ShopItemsListRow>();

            await using var rdr = await cmd.ExecuteReaderAsync();

            // helpers
            static bool Has(DbDataReader r, string col)
            { try { _ = r.GetOrdinal(col); return true; } catch { return false; } }

            static string? GetStr(DbDataReader r, string col) { if (!Has(r, col)) return null; var i = r.GetOrdinal(col); return r.IsDBNull(i) ? null : r.GetString(i); }
            static int? GetInt(DbDataReader r, string col) { if (!Has(r, col)) return null; var i = r.GetOrdinal(col); return r.IsDBNull(i) ? (int?)null : r.GetInt32(i); }
            static long? GetLong(DbDataReader r, string col) { if (!Has(r, col)) return null; var i = r.GetOrdinal(col); return r.IsDBNull(i) ? (long?)null : r.GetInt64(i); }
            static DateTime? GetDt(DbDataReader r, string col) { if (!Has(r, col)) return null; var i = r.GetOrdinal(col); return r.IsDBNull(i) ? (DateTime?)null : r.GetDateTime(i); }

            // required ordinals
            var oItemId = rdr.GetOrdinal("item_id");
            var oTypeId = rdr.GetOrdinal("item_type_id");
            var oNameKey = rdr.GetOrdinal("name_key");
            var oAvail = rdr.GetOrdinal("is_available");
            var oPrice = rdr.GetOrdinal("price_minor_units");
            var oStock = rdr.GetOrdinal("stock_quantity");
            var oCurr = rdr.GetOrdinal("currency_id");
            var hasDesc = Has(rdr, "description_key");
            var hasLocale = Has(rdr, "locale_id");

            // optional blocks present in SQL
            var hasDisc = Has(rdr, "discount_name_key");

            var hasMerch = Has(rdr, "merch_type_id");
            var hasMerchThumbId = Has(rdr, "merch_thumb_media_id");
            var hasMerchThumbType = Has(rdr, "merch_thumb_media_type_id");
            var hasMerchThumbText = Has(rdr, "merch_thumb_text_key");
            var hasMerchThumbDesc = Has(rdr, "merch_thumb_desc_key");
            var hasMerchThumbLink = Has(rdr, "merch_thumb_media_link");

            var hasTicket = Has(rdr, "ticket_type_id");

            var hasBadgeMediaId = Has(rdr, "badge_media_id");
            var hasBadgeMediaType = Has(rdr, "badge_media_type_id");
            var hasBadgeTextKey = Has(rdr, "badge_media_text_key");
            var hasBadgeDescKey = Has(rdr, "badge_media_desc_key");
            var hasBadgeLink = Has(rdr, "badge_media_link");

            while (await rdr.ReadAsync())
            {
                var itemId = rdr.GetInt32(oItemId);
                var itemTypeId = rdr.GetInt32(oTypeId);

                if (!map.TryGetValue(itemId, out var row))
                {
                    var nameKey = rdr.GetString(oNameKey);
                    var isAvailable = rdr.GetInt32(oAvail) == 1;
                    var price = rdr.IsDBNull(oPrice) ? 0L : rdr.GetInt64(oPrice);
                    var stock = rdr.GetInt32(oStock);
                    var currencyId = rdr.GetInt32(oCurr);
                    var descKey = hasDesc ? GetStr(rdr, "description_key") : null;

                    ShopItemsDiscount? discount = null;
                    if (hasDisc && !rdr.IsDBNull(rdr.GetOrdinal("discount_name_key")))
                    {
                        discount = new ShopItemsDiscount
                        {
                            Name_Key = rdr.GetString(rdr.GetOrdinal("discount_name_key")),
                            Discount_Type = GetInt(rdr, "discount_type_id") ?? 0,
                            Value_Minor_Units = GetLong(rdr, "discount_value_minor_units") ?? 0,
                            Start_At = GetDt(rdr, "discount_start_at") ?? DateTime.MinValue,
                            End_At = GetDt(rdr, "discount_end_at") ?? DateTime.MinValue
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
                            Price_Minor_Units = price,
                            Stock_Quantity = stock,
                            Currency_Id = currencyId,
                            Discount = discount
                        },
                        Merchandise = null,
                        Ticket = null,
                        Badge = null
                    };

                    // seed subtype objects (without per-locale links yet)
                    if (itemTypeId == 1 && hasMerch && !rdr.IsDBNull(rdr.GetOrdinal("merch_type_id")))
                    {
                        row.Merchandise = new MerchandiseRead
                        {
                            Merchandise_Type_Id = rdr.GetInt32(rdr.GetOrdinal("merch_type_id")),
                            Merchandise_Category_Id = GetInt(rdr, "merch_category_id"),
                            Asset_Url = GetStr(rdr, "merch_asset_url"),
                            Thumbnail = null
                        };
                    }
                    else if (itemTypeId == 2 && hasTicket && !rdr.IsDBNull(rdr.GetOrdinal("ticket_type_id")))
                    {
                        row.Ticket = new TicketRead
                        {
                            Event_Id = GetInt(rdr, "ticket_event_id") ?? 0,
                            Ticket_Type_Id = rdr.GetInt32(rdr.GetOrdinal("ticket_type_id")),
                            Seat = GetStr(rdr, "ticket_seat"),
                            Section = GetStr(rdr, "ticket_section"),
                            Valid_From = GetDt(rdr, "ticket_valid_from"),
                            Valid_To = GetDt(rdr, "ticket_valid_to"),
                            Is_Transferable = (GetInt(rdr, "ticket_is_transferable") ?? 0) == 1
                        };
                    }
                    else if (itemTypeId == 3 && hasBadgeMediaId && !rdr.IsDBNull(rdr.GetOrdinal("badge_media_id")))
                    {
                        row.Badge = new BadgeRead
                        {
                            Media = new MediaData
                            {
                                Id = rdr.GetInt32(rdr.GetOrdinal("badge_media_id")),
                                MediaTypeId = GetInt(rdr, "badge_media_type_id") ?? 0,
                                TextKey = GetStr(rdr, "badge_media_text_key"),
                                DescriptionKey = GetStr(rdr, "badge_media_desc_key"),
                                LinkLocalizations = new List<MediaLocalization>()
                            }
                        };
                    }

                    map[itemId] = row;
                }

                // Per-locale link stitching (merch thumbnail & badge media)
                if (hasLocale && !rdr.IsDBNull(rdr.GetOrdinal("locale_id")))
                {
                    var locale = rdr.GetString(rdr.GetOrdinal("locale_id"));

                    // merchandise thumbnail
                    if (row.Merchandise != null && hasMerchThumbId && !rdr.IsDBNull(rdr.GetOrdinal("merch_thumb_media_id")))
                    {
                        row.Merchandise.Thumbnail ??= new MediaData
                        {
                            Id = rdr.GetInt32(rdr.GetOrdinal("merch_thumb_media_id")),
                            MediaTypeId = GetInt(rdr, "merch_thumb_media_type_id") ?? 0,
                            TextKey = GetStr(rdr, "merch_thumb_text_key"),
                            DescriptionKey = GetStr(rdr, "merch_thumb_desc_key"),
                            LinkLocalizations = new List<MediaLocalization>()
                        };

                        if (hasMerchThumbLink)
                        {
                            var link = GetStr(rdr, "merch_thumb_media_link");
                            if (!string.IsNullOrEmpty(link) &&
                                !row.Merchandise.Thumbnail.LinkLocalizations.Any(l => l.LocaleId == locale))
                            {
                                row.Merchandise.Thumbnail.LinkLocalizations.Add(new MediaLocalization
                                {
                                    LocaleId = locale,
                                    MediaLink = link!
                                });
                            }
                        }
                    }

                    // badge media link
                    if (row.Badge?.Media != null && hasBadgeLink)
                    {
                        var link = GetStr(rdr, "badge_media_link");
                        if (!string.IsNullOrEmpty(link) &&
                            !row.Badge.Media.LinkLocalizations.Any(l => l.LocaleId == locale))
                        {
                            row.Badge.Media.LinkLocalizations.Add(new MediaLocalization
                            {
                                LocaleId = locale,
                                MediaLink = link!
                            });
                        }
                    }
                }
            }

            return map.Values.ToList();
        }






        private static ShopItemsListRow MapRow(DbDataReader rdr)
        {
            // required
            var itemId = rdr.GetInt32(rdr.GetOrdinal("item_id"));
            var itemTypeId = rdr.GetInt32(rdr.GetOrdinal("item_type_id"));
            var nameKey = rdr.GetString(rdr.GetOrdinal("name_key"));
            var isAvailable = rdr.GetInt32(rdr.GetOrdinal("is_available")) == 1;
            var priceMinor = rdr.IsDBNull(rdr.GetOrdinal("price_minor_units")) ? 0L : rdr.GetInt64(rdr.GetOrdinal("price_minor_units"));
            var stockQty = rdr.GetInt32(rdr.GetOrdinal("stock_quantity"));
            var currencyId = rdr.GetInt32(rdr.GetOrdinal("currency_id"));

            // optional
            var descKey = rdr.IsDBNull(rdr.GetOrdinal("description_key"))
                ? null
                : rdr.GetString(rdr.GetOrdinal("description_key"));

            ShopItemsDiscount? discount = null;
            if (!rdr.IsDBNull(rdr.GetOrdinal("discount_name_key")))
            {
                discount = new ShopItemsDiscount
                {
                    Name_Key = rdr.GetString(rdr.GetOrdinal("discount_name_key")),
                    Discount_Type = rdr.GetInt32(rdr.GetOrdinal("discount_type_id")),
                    Value_Minor_Units = rdr.GetInt64(rdr.GetOrdinal("discount_value_minor_units")),
                    Start_At = rdr.GetDateTime(rdr.GetOrdinal("discount_start_at")),
                    End_At = rdr.GetDateTime(rdr.GetOrdinal("discount_end_at")),
                };
            }

            // subtype projection (using current models)
            MerchandiseRead? merch = null;
            TicketRead? ticket = null;
            BadgeRead? badge = null;

            switch (itemTypeId)
            {
                case 1: // merchandise
                    if (!rdr.IsDBNull(rdr.GetOrdinal("merch_type_id")))
                    {
                        merch = new MerchandiseRead
                        {
                            Merchandise_Type_Id = rdr.GetInt32(rdr.GetOrdinal("merch_type_id")),
                            Merchandise_Category_Id = rdr.IsDBNull(rdr.GetOrdinal("merch_category_id"))
                                ? (int?)null
                                : rdr.GetInt32(rdr.GetOrdinal("merch_category_id")),
                            Asset_Url = rdr.IsDBNull(rdr.GetOrdinal("merch_asset_url"))
                                ? null
                                : rdr.GetString(rdr.GetOrdinal("merch_asset_url")),
                            // Thumbnail as MediaData (id + type; links can be stitched in a higher-level aggregation if needed)
                            Thumbnail = rdr.IsDBNull(rdr.GetOrdinal("merch_thumb_media_id"))
                                ? null
                                : new MediaData
                                {
                                    Id = rdr.GetInt32(rdr.GetOrdinal("merch_thumb_media_id")),
                                    MediaTypeId = rdr.IsDBNull(rdr.GetOrdinal("merch_thumb_media_type_id"))
                                                            ? 0
                                                            : rdr.GetInt32(rdr.GetOrdinal("merch_thumb_media_type_id")),
                                    TextKey = null,
                                    DescriptionKey = null,
                                    LinkLocalizations = new List<MediaLocalization>()
                                }
                        };
                    }
                    break;

                case 2: // ticket
                    if (!rdr.IsDBNull(rdr.GetOrdinal("ticket_type_id")))
                    {
                        ticket = new TicketRead
                        {
                            Event_Id = rdr.GetInt32(rdr.GetOrdinal("ticket_event_id")),
                            Ticket_Type_Id = rdr.GetInt32(rdr.GetOrdinal("ticket_type_id")),
                            Seat = rdr.IsDBNull(rdr.GetOrdinal("ticket_seat")) ? null : rdr.GetString(rdr.GetOrdinal("ticket_seat")),
                            Section = rdr.IsDBNull(rdr.GetOrdinal("ticket_section")) ? null : rdr.GetString(rdr.GetOrdinal("ticket_section")),
                            Valid_From = rdr.IsDBNull(rdr.GetOrdinal("ticket_valid_from")) ? (DateTime?)null : rdr.GetDateTime(rdr.GetOrdinal("ticket_valid_from")),
                            Valid_To = rdr.IsDBNull(rdr.GetOrdinal("ticket_valid_to")) ? (DateTime?)null : rdr.GetDateTime(rdr.GetOrdinal("ticket_valid_to")),
                            Is_Transferable = !rdr.IsDBNull(rdr.GetOrdinal("ticket_is_transferable"))
                                              && rdr.GetInt32(rdr.GetOrdinal("ticket_is_transferable")) == 1
                        };
                    }
                    break;

                case 3: // badge
                    if (!rdr.IsDBNull(rdr.GetOrdinal("badge_media_id")))
                    {
                        badge = new BadgeRead
                        {
                            Media = new MediaData
                            {
                                Id = rdr.GetInt32(rdr.GetOrdinal("badge_media_id")),
                                MediaTypeId = rdr.IsDBNull(rdr.GetOrdinal("badge_media_type_id"))
                                                      ? 0
                                                      : rdr.GetInt32(rdr.GetOrdinal("badge_media_type_id")),
                                TextKey = null,
                                DescriptionKey = null,
                                LinkLocalizations = new List<MediaLocalization>()
                            }
                        };
                    }
                    break;
            }

            return new ShopItemsListRow
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
                    Price_Minor_Units = priceMinor,
                    Stock_Quantity = stockQty,
                    Currency_Id = currencyId,
                    Discount = discount
                },
                Merchandise = merch,
                Ticket = ticket,
                Badge = badge
            };
        }



        // -------- CREATE --------
        //public async Task<ShopItemsListRow> AddAsync(int spaceId, int shopId, CreateShopItemDtoV2 dto, string modifiedBy)
        //{
        //    await using var conn = await _db.OpenConnectionAsync();
        //    await using var tx = await conn.BeginTransactionAsync();

        //    long newItemId = 0;
        //    var committed = false;

        //    try
        //    {
        //        // --- transactional work only ---
        //        // A) validations
        //        await using (var ex = _db.CreateCommand(conn, ShopItemsSql.ExistsShopInSpace, tx))
        //        {
        //            ex.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
        //            ex.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
        //            if (await ex.ExecuteScalarAsync() is null)
        //                throw new InvalidOperationException("Shop not found in space.");
        //        }

        //        await using (var rc = _db.CreateCommand(conn, ShopItemsSql.ExistsRegionalCurrency, tx))
        //        {
        //            rc.Parameters.Add(_db.CreateParameter("@RegionalCurrencyId", dto.RegionalCurrencyId));
        //            if (await rc.ExecuteScalarAsync() is null)
        //                throw new InvalidOperationException("Regional currency not found.");
        //        }

        //        // B) item
        //        await using (var ci = _db.CreateCommand(conn, ShopItemsSql.InsertItem, tx))
        //        {
        //            ci.Parameters.Add(_db.CreateParameter("@ItemTypeId", dto.ItemTypeId));
        //            ci.Parameters.Add(_db.CreateParameter("@EntityId", (object?)dto.EntityId ?? DBNull.Value));
        //            ci.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
        //            ci.Parameters.Add(_db.CreateParameter("@NameKey", dto.NameKey));
        //            ci.Parameters.Add(_db.CreateParameter("@DescriptionKey", (object?)dto.DescriptionKey ?? DBNull.Value));
        //            ci.Parameters.Add(_db.CreateParameter("@IsAvailable", dto.IsAvailable ? 1 : 0));
        //            ci.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
        //            newItemId = Convert.ToInt64(await ci.ExecuteScalarAsync());
        //        }

        //        // C) i18n
        //        if (dto.NameLocalizedPairs?.Values?.Count > 0)
        //            await UpsertI18nAsync(spaceId, dto.NameKey, dto.NameLocalizedPairs.Values, modifiedBy);
        //        if (!string.IsNullOrWhiteSpace(dto.DescriptionKey) && dto.DescriptionLocalizedPairs?.Values?.Count > 0)
        //            await UpsertI18nAsync(spaceId, dto.DescriptionKey!, dto.DescriptionLocalizedPairs.Values, modifiedBy);

        //        // D) subtype
        //        switch (dto.ItemTypeId)
        //        {
        //            case 1: // Merchandise
        //                {
        //                    string? thumbId = null;
        //                    if (dto.Merchandise?.ThumbnailMedia is not null)
        //                        thumbId = await InsertMediaIfAnyAsync(_db, conn, tx, spaceId, dto.Merchandise.ThumbnailMedia, modifiedBy);

        //                    await using var cm = _db.CreateCommand(conn, ShopItemsSql.InsertMerchandise, tx);
        //                    cm.Parameters.Add(_db.CreateParameter("@ItemId", newItemId));
        //                    cm.Parameters.Add(_db.CreateParameter("@MerchTypeId", dto.Merchandise!.MerchandiseTypeId));
        //                    cm.Parameters.Add(_db.CreateParameter("@MerchCatId", (object?)dto.Merchandise.MerchandiseCategoryId ?? DBNull.Value));
        //                    cm.Parameters.Add(_db.CreateParameter("@AssetUrl", (object?)dto.Merchandise.AssetUrl ?? DBNull.Value));
        //                    cm.Parameters.Add(_db.CreateParameter("@ThumbMediaId", (object?)(thumbId ?? dto.Merchandise.ThumbnailMediaId) ?? DBNull.Value));
        //                    cm.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
        //                    await cm.ExecuteNonQueryAsync();
        //                    break;
        //                }
        //            case 2: // Ticket
        //                {
        //                    await using var ct = _db.CreateCommand(conn, ShopItemsSql.InsertTicket, tx);
        //                    ct.Parameters.Add(_db.CreateParameter("@ItemId", newItemId));
        //                    ct.Parameters.Add(_db.CreateParameter("@EventId", dto.Ticket!.EventId));
        //                    ct.Parameters.Add(_db.CreateParameter("@TicketTypeId", dto.Ticket.TicketTypeId));
        //                    ct.Parameters.Add(_db.CreateParameter("@Seat", (object?)dto.Ticket.Seat ?? DBNull.Value));
        //                    ct.Parameters.Add(_db.CreateParameter("@Section", (object?)dto.Ticket.Section ?? DBNull.Value));
        //                    ct.Parameters.Add(_db.CreateParameter("@ValidFrom", (object?)dto.Ticket.ValidFrom ?? DBNull.Value));
        //                    ct.Parameters.Add(_db.CreateParameter("@ValidTo", (object?)dto.Ticket.ValidTo ?? DBNull.Value));
        //                    ct.Parameters.Add(_db.CreateParameter("@IsTransferable", dto.Ticket.IsTransferable ? 1 : 0));
        //                    ct.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
        //                    await ct.ExecuteNonQueryAsync();
        //                    break;
        //                }
        //            case 3: // Badge
        //                {
        //                    await using var cb = _db.CreateCommand(conn, ShopItemsSql.InsertBadge, tx);
        //                    cb.Parameters.Add(_db.CreateParameter("@ItemId", newItemId));
        //                    cb.Parameters.Add(_db.CreateParameter("@NameKey", dto.NameKey));
        //                    cb.Parameters.Add(_db.CreateParameter("@DescriptionKey", (object?)dto.DescriptionKey ?? DBNull.Value));
        //                    cb.Parameters.Add(_db.CreateParameter("@MediaId", (object?)dto.Badge?.MediaId ?? DBNull.Value));
        //                    cb.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
        //                    await cb.ExecuteNonQueryAsync();
        //                    break;
        //                }
        //            default:
        //                throw new InvalidOperationException($"Unsupported ItemTypeId: {dto.ItemTypeId}");
        //        }

        //        // E) base price
        //        await using (var ip = _db.CreateCommand(conn, ShopItemsSql.InsertItemPrice, tx))
        //        {
        //            ip.Parameters.Add(_db.CreateParameter("@ItemId", newItemId));
        //            ip.Parameters.Add(_db.CreateParameter("@RegionalCurrencyId", dto.RegionalCurrencyId));
        //            ip.Parameters.Add(_db.CreateParameter("@BaseCost", dto.BaseCost));
        //            ip.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
        //            await ip.ExecuteNonQueryAsync();
        //        }

        //        // F) mapping
        //        try
        //        {
        //            await using var insMap = _db.CreateCommand(conn, ShopItemsSql.InsertShopItem, tx);
        //            insMap.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
        //            insMap.Parameters.Add(_db.CreateParameter("@ItemId", newItemId));
        //            insMap.Parameters.Add(_db.CreateParameter("@RegionalCurrencyId", dto.RegionalCurrencyId));
        //            insMap.Parameters.Add(_db.CreateParameter("@Amount", (object?)dto.Amount ?? DBNull.Value));
        //            insMap.Parameters.Add(_db.CreateParameter("@StockQuantity", (object?)dto.StockQuantity ?? DBNull.Value));
        //            insMap.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
        //            await insMap.ExecuteNonQueryAsync();
        //        }
        //        catch (Exception ex) when (ex.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)
        //                                || ex.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
        //        {
        //            throw new InvalidOperationException("Duplicate mapping (shop_id, item_id, regional_currency_id).", ex);
        //        }

        //        await tx.CommitAsync();
        //        committed = true;
        //    }
        //    catch
        //    {
        //        if (!committed && tx?.Connection is not null)
        //            await tx.RollbackAsync();
        //        throw;
        //    }

        //    // --- post-commit work (no rollback here) ---
        //    await using var sel = _db.CreateCommand(conn, ShopItemsSql.SelectCreatedShopItemEnvelope);
        //    sel.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
        //    sel.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
        //    sel.Parameters.Add(_db.CreateParameter("@ItemId", newItemId));
        //    sel.Parameters.Add(_db.CreateParameter("@RegionalCurrencyId", dto.RegionalCurrencyId));

        //    await using var rdr2 = await sel.ExecuteReaderAsync();
        //    if (!await rdr2.ReadAsync())
        //        throw new InvalidOperationException("Created mapping not found.");

        //    return MapRow(rdr2);
        //}
        // 
        public async Task<ShopItemsListRow> AddLinkAsync(int spaceId, int shopId, CreateShopItemLinkDto dto, string modifiedBy)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) shop exists in space
                await using (var ex = _db.CreateCommand(conn, ShopItemsSql.ExistsShopInSpace, tx))
                {
                    ex.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
                    ex.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    if (await ex.ExecuteScalarAsync() is null)
                        throw new InvalidOperationException("Shop not found in space.");
                }

                // 2) item snapshot (confirms item belongs to this space and gives keys)
                ItemNameDescSnapshot? snap;
                await using (var it = _db.CreateCommand(conn, ShopItemsSql.GetItemSnapshot, tx))
                {
                    it.Parameters.Add(_db.CreateParameter("@ItemId", dto.ItemId));
                    await using var rdr = await it.ExecuteReaderAsync();
                    if (!await rdr.ReadAsync())
                        throw new InvalidOperationException($"Item not found. id={dto.ItemId}");

                    snap = new ItemNameDescSnapshot
                    {
                        Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                        SpaceId = rdr.GetInt32(rdr.GetOrdinal("SpaceId")),
                        NameKey = rdr.GetString(rdr.GetOrdinal("NameKey")),
                        DescriptionKey = rdr.IsDBNull(rdr.GetOrdinal("DescriptionKey"))
                            ? null
                            : rdr.GetString(rdr.GetOrdinal("DescriptionKey"))
                    };
                }
                if (snap!.SpaceId != spaceId)
                    throw new InvalidOperationException("Item does not belong to this space.");

                // 3) validate regional currency
                await using (var rc = _db.CreateCommand(conn, ShopItemsSql.ExistsRegionalCurrency, tx))
                {
                    rc.Parameters.Add(_db.CreateParameter("@RegionalCurrencyId", dto.RegionalCurrencyId));
                    if (await rc.ExecuteScalarAsync() is null)
                        throw new InvalidOperationException("Regional currency not found.");
                }

                // 4) ensure base price exists for this item+regional currency
                await using (var bp = _db.CreateCommand(conn, ShopItemsSql.ExistsItemPriceForCurrency, tx))
                {
                    bp.Parameters.Add(_db.CreateParameter("@ItemId", dto.ItemId));
                    bp.Parameters.Add(_db.CreateParameter("@RegionalCurrencyId", dto.RegionalCurrencyId));
                    if (await bp.ExecuteScalarAsync() is null)
                        throw new InvalidOperationException("Base price not found for item in this regional currency (item_prices).");
                }

                // 5) optional i18n upsert
                if (dto.NameLocalizedPairs?.Values?.Count > 0)
                    await UpsertI18nAsync(spaceId, snap.NameKey, dto.NameLocalizedPairs.Values, modifiedBy);
                if (dto.DescriptionLocalizedPairs?.Values?.Count > 0 && !string.IsNullOrWhiteSpace(snap.DescriptionKey))
                    await UpsertI18nAsync(spaceId, snap.DescriptionKey!, dto.DescriptionLocalizedPairs.Values, modifiedBy);

                // 6a) create shop_items mapping (override + stock)
                try
                {
                    await using var insMap = _db.CreateCommand(conn, ShopItemsSql.InsertShopItem, tx);
                    insMap.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
                    insMap.Parameters.Add(_db.CreateParameter("@ItemId", dto.ItemId));
                    insMap.Parameters.Add(_db.CreateParameter("@RegionalCurrencyId", dto.RegionalCurrencyId));
                    insMap.Parameters.Add(_db.CreateParameter("@Amount", (object?)dto.Amount ?? DBNull.Value));
                    insMap.Parameters.Add(_db.CreateParameter("@StockQuantity", (object?)dto.StockQuantity ?? DBNull.Value));
                    insMap.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                    await insMap.ExecuteNonQueryAsync();
                }
                catch (Exception ex) when (ex.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase) ||
                                           ex.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Duplicate mapping (shop_id, item_id, regional_currency_id).", ex);
                }

                // 6b) OPTIONAL: upsert discount mapping for this item
                if (dto.Discount is not null)
                {
                    // (Optional) validate discount exists;
                    const string existsDiscountSql = @"SELECT 1 FROM discounts WHERE id = @DiscountId LIMIT 1;";
                    await using (var chk = _db.CreateCommand(conn, existsDiscountSql, tx))
                    {
                        chk.Parameters.Add(_db.CreateParameter("@DiscountId", dto.Discount.DiscountId));
                        if (await chk.ExecuteScalarAsync() is null)
                            throw new InvalidOperationException($"Discount not found. id={dto.Discount.DiscountId}");
                    }

                    // UPDATE first
                    const string upd = @"
UPDATE item_discounts
   SET discount_id   = @DiscountId,
       modified_by   = @ModifiedBy,
       modified_time = UTC_TIMESTAMP()
 WHERE item_id = @ItemId
   AND ( (@RegionId IS NULL AND region_id IS NULL) OR region_id = @RegionId );";

                    int affected;
                    await using (var up = _db.CreateCommand(conn, upd, tx))
                    {
                        up.Parameters.Add(_db.CreateParameter("@ItemId", dto.ItemId));
                        up.Parameters.Add(_db.CreateParameter("@RegionId", (object?)dto.Discount.RegionId ?? DBNull.Value));
                        up.Parameters.Add(_db.CreateParameter("@DiscountId", dto.Discount.DiscountId));
                        up.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                        affected = await up.ExecuteNonQueryAsync();
                    }

                    // If no row to update → INSERT
                    if (affected == 0)
                    {
                        const string ins = @"
INSERT INTO item_discounts (item_id, region_id, discount_id, creation_time, modified_time, modified_by)
VALUES (@ItemId, @RegionId, @DiscountId, UTC_TIMESTAMP(), UTC_TIMESTAMP(), @ModifiedBy);";

                        await using var insCmd = _db.CreateCommand(conn, ins, tx);
                        insCmd.Parameters.Add(_db.CreateParameter("@ItemId", dto.ItemId));
                        insCmd.Parameters.Add(_db.CreateParameter("@RegionId", (object?)dto.Discount.RegionId ?? DBNull.Value));
                        insCmd.Parameters.Add(_db.CreateParameter("@DiscountId", dto.Discount.DiscountId));
                        insCmd.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                        await insCmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();

                // 7) Return the created envelope
                return await GetOneAsync(spaceId, shopId, dto.ItemId, dto.RegionalCurrencyId);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }


        public async Task<ShopItemsListRow> GetOneAsync(int spaceId, int shopId, long itemId, int regionalCurrencyId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopItemsSql.SelectOneByShop);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
            cmd.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
            cmd.Parameters.Add(_db.CreateParameter("@RegionalCurrencyId", regionalCurrencyId));

            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync())
                throw new InvalidOperationException("Created mapping not found.");

            return MapRow(rdr);
        }

        public async Task UpsertItemDiscountAsync(long itemId, int? regionId, int discountId, string modifiedBy)
        {
            await using var conn = await _db.OpenConnectionAsync();

            // 1) Try update existing
            const string updateSql = @"
        UPDATE item_discounts
           SET discount_id  = @DiscountId,
               modified_by  = @ModifiedBy,
               modified_time = UTC_TIMESTAMP()
         WHERE item_id = @ItemId
           AND (
                 (@RegionId IS NULL AND region_id IS NULL)
                 OR region_id = @RegionId
               );";

            int affected;
            await using (var up = _db.CreateCommand(conn, updateSql))
            {
                up.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
                up.Parameters.Add(_db.CreateParameter("@RegionId", (object?)regionId ?? DBNull.Value));
                up.Parameters.Add(_db.CreateParameter("@DiscountId", discountId));
                up.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                affected = await up.ExecuteNonQueryAsync();
            }

            if (affected > 0) return;

            // 2) Insert new mapping
            const string insertSql = @"
        INSERT INTO item_discounts
            (item_id, region_id, discount_id, creation_time, modified_time, modified_by)
        VALUES
            (@ItemId, @RegionId, @DiscountId, UTC_TIMESTAMP(), UTC_TIMESTAMP(), @ModifiedBy);";

            await using (var ins = _db.CreateCommand(conn, insertSql))
            {
                ins.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
                ins.Parameters.Add(_db.CreateParameter("@RegionId", (object?)regionId ?? DBNull.Value));
                ins.Parameters.Add(_db.CreateParameter("@DiscountId", discountId));
                ins.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                await ins.ExecuteNonQueryAsync();
            }
        }



        public async Task<ItemNameDescSnapshot?> GetItemSnapshotAsync(int itemId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopItemsSql.GetItemSnapshot);
            cmd.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!(await rdr.ReadAsync())) return null;
            return new ItemNameDescSnapshot
            {
                Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                SpaceId = rdr.GetInt32(rdr.GetOrdinal("SpaceId")),
                NameKey = rdr.GetString(rdr.GetOrdinal("NameKey")),
                DescriptionKey = rdr.IsDBNull(rdr.GetOrdinal("DescriptionKey")) ? null : rdr.GetString(rdr.GetOrdinal("DescriptionKey"))
            };
        }

        public async Task<bool> RegionalCurrencyExistsAsync(int regionalCurrencyId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopItemsSql.ExistsRegionalCurrency);
            cmd.Parameters.Add(_db.CreateParameter("@RegionalCurrencyId", regionalCurrencyId));
            var obj = await cmd.ExecuteScalarAsync();
            return obj is not null;
        }

        public async Task<HashSet<string>> GetSupportedLocalesAsync(int spaceId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopItemsSql.GetSupportedLocales);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                set.Add(rdr.GetString(0));
            return set;
        }

        public async Task<int> UpsertI18nAsync(int spaceId, string key, IEnumerable<LocalizedValue> pairs, string modifiedBy)
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

        //private static ShopItemsListRow MapRow(DbDataReader rdr)
        //{
        //    ShopItemsDiscount? discount = null;
        //    if (!rdr.IsDBNull(rdr.GetOrdinal("Discount_Name_Key")))
        //    {
        //        discount = new ShopItemsDiscount
        //        {
        //            Name_Key = rdr.GetString(rdr.GetOrdinal("Discount_Name_Key")),
        //            Discount_Type = rdr.IsDBNull(rdr.GetOrdinal("Discount_Type")) ? 0 : rdr.GetInt32(rdr.GetOrdinal("Discount_Type")),
        //            Value_Minor_Units = rdr.IsDBNull(rdr.GetOrdinal("Discount_Value_Minor_Units")) ? 0 : rdr.GetInt64(rdr.GetOrdinal("Discount_Value_Minor_Units")),
        //            Start_At = rdr.IsDBNull(rdr.GetOrdinal("Discount_Start_At")) ? default : rdr.GetDateTime(rdr.GetOrdinal("Discount_Start_At")),
        //            End_At = rdr.IsDBNull(rdr.GetOrdinal("Discount_End_At")) ? default : rdr.GetDateTime(rdr.GetOrdinal("Discount_End_At"))
        //        };
        //    }

        //    return new ShopItemsListRow
        //    {
        //        Item = new ShopItemsItem
        //        {
        //            Id = rdr.GetInt32(rdr.GetOrdinal("Id")),
        //            Item_Type_Id = rdr.GetInt32(rdr.GetOrdinal("Item_Type_Id")),
        //            Name_Key = rdr.GetString(rdr.GetOrdinal("Name_Key")),
        //            Description_Key = rdr.IsDBNull(rdr.GetOrdinal("Description_Key")) ? null : rdr.GetString(rdr.GetOrdinal("Description_Key")),
        //            Is_Available = rdr.GetBoolean(rdr.GetOrdinal("Is_Available"))
        //        },
        //        Shop_Item = new ShopItemsShopPart
        //        {
        //            Price_Minor_Units = rdr.GetInt64(rdr.GetOrdinal("Price_Minor_Units")),
        //            Stock_Quantity = rdr.GetInt32(rdr.GetOrdinal("Stock_Quantity")),
        //            Currency_Id = rdr.GetInt32(rdr.GetOrdinal("Currency_Id")),
        //            Discount = discount
        //        }
        //    };
        //}

        public async Task<bool> DiscountExistsAsync(int discountId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopItemsSql.ExistsDiscount);
            cmd.Parameters.Add(_db.CreateParameter("@DiscountId", discountId));
            var obj = await cmd.ExecuteScalarAsync();
            return obj is not null;
        }

        public async Task UpsertItemDiscountAsync(int itemId, int? regionId, int discountId, string modifiedBy)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopItemsSql.UpsertItemDiscount);
            cmd.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
            cmd.Parameters.Add(_db.CreateParameter("@RegionId", (object?)regionId ?? DBNull.Value));
            cmd.Parameters.Add(_db.CreateParameter("@DiscountId", discountId));
            cmd.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> MappingExistsInSpaceAsync(int spaceId, int shopId, int itemId, int regionalCurrencyId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopItemsSql.ExistsMappingInSpace);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
            cmd.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
            cmd.Parameters.Add(_db.CreateParameter("@RegionalCurrencyId", regionalCurrencyId));
            var obj = await cmd.ExecuteScalarAsync();
            return obj is not null;
        }

        public async Task<ShopItemsListRow> UpdateAsync(int spaceId, int shopId, int itemId, UpdateShopItemDto dto, string modifiedBy)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // optional i18n upserts
                if (dto.NameLocalizedPairs?.Values is { Count: > 0 })
                {
                    foreach (var v in dto.NameLocalizedPairs.Values)
                    {
                        // try update
                        await using (var u = _db.CreateCommand(conn, ShopItemsSql.UpdateI18nValue, tx))
                        {
                            u.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                            u.Parameters.Add(_db.CreateParameter("@Key", dto.NameLocalizedPairs.Key!));
                            u.Parameters.Add(_db.CreateParameter("@LocaleId", v.LocaleId));
                            u.Parameters.Add(_db.CreateParameter("@Value", v.Value));
                            u.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                            var rows = await u.ExecuteNonQueryAsync();

                            if (rows == 0)
                            {
                                await using var ins = _db.CreateCommand(conn, ShopItemsSql.InsertI18nValue, tx);
                                ins.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                                ins.Parameters.Add(_db.CreateParameter("@Key", dto.NameLocalizedPairs.Key!));
                                ins.Parameters.Add(_db.CreateParameter("@LocaleId", v.LocaleId));
                                ins.Parameters.Add(_db.CreateParameter("@Value", v.Value));
                                ins.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                                await ins.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace((await GetItemSnapshotAsync(itemId))?.DescriptionKey) &&
                    dto.DescriptionLocalizedPairs?.Values is { Count: > 0 })
                {
                    foreach (var v in dto.DescriptionLocalizedPairs.Values)
                    {
                        await using (var u = _db.CreateCommand(conn, ShopItemsSql.UpdateI18nValue, tx))
                        {
                            u.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                            u.Parameters.Add(_db.CreateParameter("@Key", dto.DescriptionLocalizedPairs.Key!));
                            u.Parameters.Add(_db.CreateParameter("@LocaleId", v.LocaleId));
                            u.Parameters.Add(_db.CreateParameter("@Value", v.Value));
                            u.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                            var rows = await u.ExecuteNonQueryAsync();

                            if (rows == 0)
                            {
                                await using var ins = _db.CreateCommand(conn, ShopItemsSql.InsertI18nValue, tx);
                                ins.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                                ins.Parameters.Add(_db.CreateParameter("@Key", dto.DescriptionLocalizedPairs.Key!));
                                ins.Parameters.Add(_db.CreateParameter("@LocaleId", v.LocaleId));
                                ins.Parameters.Add(_db.CreateParameter("@Value", v.Value));
                                ins.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                                await ins.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                // update mapping
                await using (var up = _db.CreateCommand(conn, ShopItemsSql.UpdateShopItem, tx))
                {
                    up.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
                    up.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
                    up.Parameters.Add(_db.CreateParameter("@RegionalCurrencyId", dto.RegionalCurrencyId));
                    up.Parameters.Add(_db.CreateParameter("@Amount", (object?)dto.Amount ?? DBNull.Value));
                    up.Parameters.Add(_db.CreateParameter("@StockQuantity", (object?)dto.StockQuantity ?? DBNull.Value));
                    up.Parameters.Add(_db.CreateParameter("@ModifiedBy", modifiedBy));
                    await up.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                // return updated envelope (same selector used for creates)
                await using var sel = _db.CreateCommand(conn, ShopItemsSql.SelectUpdatedShopItemEnvelope);
                sel.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                sel.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
                sel.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
                sel.Parameters.Add(_db.CreateParameter("@RegionalCurrencyId", dto.RegionalCurrencyId));

                await using var rdr = await sel.ExecuteReaderAsync();
                if (!await rdr.ReadAsync())
                    throw new InvalidOperationException("Updated shop item not found.");

                return MapShopItemsListRow(rdr);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private static ShopItemsListRow MapShopItemsListRow(DbDataReader rdr) => new()
        {
            Item = new ShopItemsItem
            {
                Id = rdr.GetInt32(rdr.GetOrdinal("Id")),
                Item_Type_Id = rdr.GetInt32(rdr.GetOrdinal("Item_Type_Id")),
                Name_Key = rdr.GetString(rdr.GetOrdinal("Name_Key")),
                Description_Key = rdr.IsDBNull(rdr.GetOrdinal("Description_Key")) ? null : rdr.GetString(rdr.GetOrdinal("Description_Key")),
                Is_Available = rdr.GetBoolean(rdr.GetOrdinal("Is_Available"))
            },
            Shop_Item = new ShopItemsShopPart
            {
                Price_Minor_Units = rdr.GetInt64(rdr.GetOrdinal("Price_Minor_Units")),
                Stock_Quantity = rdr.GetInt32(rdr.GetOrdinal("Stock_Quantity")),
                Currency_Id = rdr.GetInt32(rdr.GetOrdinal("Currency_Id")),
                Discount = rdr.IsDBNull(rdr.GetOrdinal("Discount_Name_Key")) ? null : new ShopItemsDiscount
                {
                    Name_Key = rdr.GetString(rdr.GetOrdinal("Discount_Name_Key")),
                    Discount_Type = rdr.GetInt32(rdr.GetOrdinal("Discount_Type")),
                    Value_Minor_Units = rdr.GetInt64(rdr.GetOrdinal("Discount_Value_Minor_Units")),
                    Start_At = rdr.GetDateTime(rdr.GetOrdinal("Discount_Start_At")),
                    End_At = rdr.GetDateTime(rdr.GetOrdinal("Discount_End_At"))
                }
            }
        };


        public async Task DeleteMappingAsync(int spaceId, int shopId, int itemId, int regionalCurrencyId, string modifiedBy)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var cmd = _db.CreateCommand(conn, ShopItemsSql.DeleteMapping);
            cmd.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
            cmd.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
            cmd.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
            cmd.Parameters.Add(_db.CreateParameter("@RegionalCurrencyId", regionalCurrencyId));
            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0)
                throw new InvalidOperationException("No rows deleted (mapping missing).");
        }

        public async Task<bool> DeleteShopItemAndDiscountsAsync(int spaceId, int shopId, int itemId)
        {
            await using var conn = await _db.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // (A) Ensure the shop belongs to space
                await using (var ex = _db.CreateCommand(conn, ShopItemsSql.ExistsShopInSpace, tx))
                {
                    ex.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
                    ex.Parameters.Add(_db.CreateParameter("@SpaceId", spaceId));
                    if (await ex.ExecuteScalarAsync() is null)
                        return false; // shop not in space
                }

                // (B) Delete mapping(s) for this shop + item (all currencies)
                int affectedShopItems;
                await using (var delMap = _db.CreateCommand(conn, ShopItemsSql.DeleteShopItemsForShopItem, tx))
                {
                    delMap.Parameters.Add(_db.CreateParameter("@ShopId", shopId));
                    delMap.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
                    affectedShopItems = await delMap.ExecuteNonQueryAsync();
                }

                // (C) Delete any discounts tied to this item (all regions)
                int affectedDiscounts;
                await using (var delDisc = _db.CreateCommand(conn, ShopItemsSql.DeleteItemDiscountsByItem, tx))
                {
                    delDisc.Parameters.Add(_db.CreateParameter("@ItemId", itemId));
                    affectedDiscounts = await delDisc.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                // return true if it actually removed at least one shop_items row
                return affectedShopItems > 0 || affectedDiscounts > 0;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }



    //    private static async Task<string?> InsertMediaIfAnyAsync(
    //IDbProvider db, DbConnection conn, DbTransaction tx,
    //int spaceId, MediaCreateDto media, string modifiedBy)
    //    {
    //        // fetch supported locales for this space
    //        var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    //        await using (var cmd = db.CreateCommand(conn, ShopItemsSql.SelectSupportedLocales, tx))
    //        {
    //            cmd.Parameters.Add(db.CreateParameter("@SpaceId", spaceId));
    //            await using var r = await cmd.ExecuteReaderAsync();
    //            while (await r.ReadAsync()) supported.Add(r.GetString(0));
    //        }

    //        // filter localizations to supported locales
    //        media.LinkLocalizations = media.LinkLocalizations?.Where(x => supported.Contains(x.LocaleId)).ToList();
    //        media.TextLocalizations = media.TextLocalizations?.Where(x => supported.Contains(x.LocaleId)).ToList();
    //        media.DescriptionLocalizations = media.DescriptionLocalizations?.Where(x => supported.Contains(x.LocaleId)).ToList();

    //        if (media.LinkLocalizations is null || media.LinkLocalizations.Count == 0)
    //            return null;

    //        // allocate id
    //        string mediaId;
    //        await using (var getId = db.CreateCommand(conn, ShopItemsSql.SelectUuid, tx))
    //        {
    //            mediaId = Convert.ToString(await getId.ExecuteScalarAsync())!;
    //        }

    //        // media row
    //        await using (var ins = db.CreateCommand(conn, ShopItemsSql.InsertMedia, tx))
    //        {
    //            ins.Parameters.Add(db.CreateParameter("@Id", mediaId));
    //            ins.Parameters.Add(db.CreateParameter("@SpaceId", spaceId));
    //            ins.Parameters.Add(db.CreateParameter("@MediaTypeId", media.MediaTypeId));
    //            ins.Parameters.Add(db.CreateParameter("@TextKey", (object?)media.TextKey ?? DBNull.Value));
    //            ins.Parameters.Add(db.CreateParameter("@DescKey", (object?)media.DescriptionKey ?? DBNull.Value));
    //            await ins.ExecuteNonQueryAsync();
    //        }

    //        // i18n for text/description
    //        if (media.TextKey is not null && media.TextLocalizations is not null)
    //        {
    //            foreach (var loc in media.TextLocalizations)
    //            {
    //                await using var ii = db.CreateCommand(conn, ShopItemsSql.InsertI18nBare, tx);
    //                ii.Parameters.Add(db.CreateParameter("@Key", media.TextKey));
    //                ii.Parameters.Add(db.CreateParameter("@LocaleId", loc.LocaleId));
    //                ii.Parameters.Add(db.CreateParameter("@Value", loc.Value));
    //                ii.Parameters.Add(db.CreateParameter("@SpaceId", spaceId));
    //                await ii.ExecuteNonQueryAsync();
    //            }
    //        }
    //        if (media.DescriptionKey is not null && media.DescriptionLocalizations is not null)
    //        {
    //            foreach (var loc in media.DescriptionLocalizations)
    //            {
    //                await using var ii = db.CreateCommand(conn, ShopItemsSql.InsertI18nBare, tx);
    //                ii.Parameters.Add(db.CreateParameter("@Key", media.DescriptionKey));
    //                ii.Parameters.Add(db.CreateParameter("@LocaleId", loc.LocaleId));
    //                ii.Parameters.Add(db.CreateParameter("@Value", loc.Value));
    //                ii.Parameters.Add(db.CreateParameter("@SpaceId", spaceId));
    //                await ii.ExecuteNonQueryAsync();
    //            }
    //        }

    //        // media_localization links
    //        if (media.LinkLocalizations is not null)
    //        {
    //            foreach (var loc in media.LinkLocalizations)
    //            {
    //                await using var il = db.CreateCommand(conn, ShopItemsSql.InsertMediaLocalization, tx);
    //                il.Parameters.Add(db.CreateParameter("@MediaId", mediaId));
    //                il.Parameters.Add(db.CreateParameter("@LocaleId", loc.LocaleId));
    //                il.Parameters.Add(db.CreateParameter("@MediaLink", loc.MediaLink));
    //                await il.ExecuteNonQueryAsync();
    //            }
    //        }

    //        return mediaId;
    //    }

        public async Task<ShopItemsGroupedResponse> ListByShopGroupedAsync(int spaceId, int shopId)
        {
            var flatRows = await ListByShopAsync(spaceId, shopId);
            return ProjectToGrouped(flatRows);
        }

        // private async Task<IReadOnlyList<ShopItemsListRow>> ListByShopAsync(int spaceId, int shopId) {}

        private static ShopItemsGroupedResponse ProjectToGrouped(IReadOnlyList<ShopItemsListRow> rows)
        {
            var resp = new ShopItemsGroupedResponse();

            foreach (var r in rows)
            {
                var price = new ShopItemPriceOut
                {
                    priceMinorUnits = r.Shop_Item.Price_Minor_Units,
                    stockQuantity = r.Shop_Item.Stock_Quantity,
                    currencyId = r.Shop_Item.Currency_Id,
                    discount = r.Shop_Item.Discount is null ? null : new DiscountOut
                    {
                        nameKey = r.Shop_Item.Discount.Name_Key,
                        discountType = r.Shop_Item.Discount.Discount_Type,
                        valueMinorUnits = r.Shop_Item.Discount.Value_Minor_Units,
                        startAt = r.Shop_Item.Discount.Start_At,
                        endAt = r.Shop_Item.Discount.End_At
                    }
                };

                switch (r.Item.Item_Type_Id)
                {
                    case 1: // merchandise
                        resp.MerchandiseList.Add(new MerchOut
                        {
                            id = r.Item.Id,
                            itemTypeId = r.Item.Item_Type_Id,
                            nameKey = r.Item.Name_Key,
                            descriptionKey = r.Item.Description_Key,
                            isAvailable = r.Item.Is_Available,
                            shopItem = price,
                            merchandiseTypeId = r.Merchandise?.Merchandise_Type_Id ?? 0,
                            merchandiseCategoryId = r.Merchandise?.Merchandise_Category_Id,
                            assetUrl = r.Merchandise?.Asset_Url,
                            thumbnail = r.Merchandise?.Thumbnail
                        });
                        break;

                    case 2: // ticket
                        resp.TicketsList.Add(new TicketOut
                        {
                            id = r.Item.Id,
                            itemTypeId = r.Item.Item_Type_Id,
                            nameKey = r.Item.Name_Key,
                            descriptionKey = r.Item.Description_Key,
                            isAvailable = r.Item.Is_Available,
                            shopItem = price,

                            eventId = r.Ticket?.Event_Id ?? 0,
                            ticketTypeId = r.Ticket?.Ticket_Type_Id ?? 0,
                            seat = r.Ticket?.Seat,
                            section = r.Ticket?.Section,
                            validFrom = r.Ticket?.Valid_From,
                            validTo = r.Ticket?.Valid_To,
                            isTransferable = r.Ticket?.Is_Transferable ?? false
                        });
                        break;

                    case 3: // badge
                        resp.BadgesList.Add(new BadgeOut
                        {
                            id = r.Item.Id,
                            itemTypeId = r.Item.Item_Type_Id,
                            nameKey = r.Item.Name_Key,
                            descriptionKey = r.Item.Description_Key,
                            isAvailable = r.Item.Is_Available,
                            shopItem = price,
                            media = r.Badge?.Media
                        });
                        break;
                }
            }

            return resp;
        }

    }
}
