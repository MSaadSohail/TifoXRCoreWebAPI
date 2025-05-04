using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Numerics;
using System.Runtime.Intrinsics.Arm;

namespace TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TifoXRCoreWebController : ControllerBase
    {
        private const int MediaImage = 0;
        private const int MediaVideo = 1;

        private const string None = "None";

        // Unity issues with JSON
        // https://stackoverflow.com/questions/36239705/serialize-and-deserialize-json-and-json-array-in-unity
        private const string TifoXRSpace = "Items";

        private const string Error001 = "Error 001: Unknown or invalid TifoXR space specified. Please contact GMS support@globalmobisoft.com.";
        private const string Error002 = "Error 002: Unknown or invalid TifoXR user name specified. Please contact GMS support@globalmobisoft.com.";
        private const string Error003 = "Error 003: Unknown or invalid TifoXR challenge score specified. Please contact GMS support@globalmobisoft.com.";
        private const string Error004 = "Error 004: Unknown or invalid TifoXR challenge team specified. Please contact GMS support@globalmobisoft.com.";
        private const string Error005 = "Error 005: Unknown or invalid TifoXR challenge name specified. Please contact GMS support@globalmobisoft.com.";

        private const string Success = "Successful TifoXR challenge score update.";

        private const string Score = "score";
        private const string UserName = "user_name";
        private const string IpAddress = "ip_address";
        private const string TimeStamp = "time_stamp";
        private const string SupportedTeam = "supported_team";
        private const string ChallengeName = "challenge_name";

        // Current Challenge Names
        // DI = Derby d'Italia
        // DM = Derby della Madonnina
        // EC = El Classico

        private const string DefaultConnectionString = "Server=tcp:globalmobilesoftware.database.windows.net,1433;Initial Catalog=TifoXR;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Default\";";

        private const string ConnectionString = DefaultConnectionString;

        private static Dictionary<string, string> spaceDictionary = new Dictionary<string, string>();

        private readonly ILogger<TifoXRCoreWebController> _logger;

        public TifoXRCoreWebController(ILogger<TifoXRCoreWebController> logger)
        {
            _logger = logger;
        }

        //
        // https://localhost:7137/TifoXRCoreWeb/GetTeamScore?space_sku=674b943c7098e92c4f8e304c&supported_team=Inter&challenge_name=DI
        // https://tifoxrcorewebapi.azurewebsites.net/TifoXRCoreWeb/GetTeamScore?space_sku=674b943c7098e92c4f8e304c&supported_team=Inter&challenge_name=DI
        //
        [HttpGet("GetTeamScore")]
        public string GetTeamScore(string space_sku, string supported_team, string challenge_name)
        {
            if (string.IsNullOrEmpty(space_sku))
                return Error001;

            if (string.IsNullOrEmpty(challenge_name))
                return Error005;

            return ExecuteSumAzureSql(space_sku, supported_team, challenge_name);
        }

        private string ExecuteSumAzureSql(string space_sku, string supportedTeam, string challengeName)
        {
            // Use your own values for Server, Database, and User Id.
            // With Microsoft.Data.SqlClient v5.2+
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();

                if (!IsValidSpace(conn, space_sku))
                    return Error001;

                string sumQuery = string.Format("select SUM(score) from tifoxr_challenge tc where sku = '{0}' and supported_team = '{1}' and challenge_name ='{2}';", space_sku, supportedTeam, challengeName);
                var command = new SqlCommand(sumQuery, conn);
                Decimal sum = 0;
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    reader.Read();
                    sum = reader.GetDecimal(0);
                    reader.Close();
                }

                conn.Close();

                return sum.ToString();
            }
        }

        //
        // https://localhost:7137/TifoXRCoreWeb/RecordUserScore?space_sku=674b943c7098e92c4f8e304c&user_name=user&ip_address=198.0.0.1&score=5&supported_team=Inter&challenge_name=DI
        // https://tifoxrcorewebapi.azurewebsites.net/TifoXRCoreWeb/RecordUserScore?space_sku=674b943c7098e92c4f8e304c&user_name=user&ip_address=198.0.0.1&score=5&supported_team=Inter&challenge_name=DI
        //
        [HttpGet("RecordUserScore")]
        public string RecordUserScore(string space_sku, string user_name, string ip_address, int score, string supported_team, string challenge_name)
        {
            if (string.IsNullOrEmpty(space_sku))
                return Error001;

            if (string.IsNullOrEmpty(user_name))
                return Error002;

            string ipAddress;
            if (string.IsNullOrEmpty(ip_address))
                ipAddress = None;

            else
                ipAddress = ip_address;

            if (score < 0)
                return Error003;

            if (string.IsNullOrEmpty(supported_team))
                return Error004;

            if (string.IsNullOrEmpty(challenge_name))
                return Error005;

            string timeStamp = GetTimestamp(DateTime.Now);

            return ExecuteInsertAzureSql(space_sku, ipAddress, user_name, timeStamp, score.ToString(), supported_team, challenge_name);
        }

        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }

        private bool IsValidSpace(SqlConnection conn, string space_sku)
        {
            if (string.IsNullOrEmpty(space_sku))
                return false;

            string selectQuery = "SELECT sku, name FROM tifoxr_space;";
            SqlCommand command = new SqlCommand(selectQuery, conn);
            spaceDictionary.Clear();
            string? key = null, value;
            using (SqlDataReader rdr = command.ExecuteReader())
            {
                while (rdr.Read())
                {
                    key = rdr.GetString(0);
                    value = rdr.GetString(1);
                    spaceDictionary.Add(key, value);
                }
            }

            if (string.IsNullOrEmpty(key))
                return false;

            return !string.IsNullOrEmpty(spaceDictionary[space_sku]);
        }

        private string ExecuteInsertAzureSql(string space_sku, string ipAddress, string user_name, string timeStamp, string score, string supported_team, string challenge_name)
        {
            // Use your own values for Server, Database, and User Id.
            // With Microsoft.Data.SqlClient v5.2+
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();

                if (!IsValidSpace(conn, space_sku))
                    return Error001;

                string insertQuery = string.Format("INSERT INTO tifoxr_challenge (sku, ip_address, user_name, time_stamp, score, supported_team, challenge_name) VALUES ('{0}', '{1}', '{2}', '{3}', {4}, '{5}', '{6}');", space_sku, ipAddress, user_name, timeStamp, score, supported_team, challenge_name);
                var command = new SqlCommand(insertQuery, conn);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    reader.Close();
                }

                conn.Close();

                return Success;
            }
        }

        [HttpGet("GetMediaData")]
        public Dictionary<string, List<MediaData>>? GetMediaData(string space_sku)
        {
            if (string.IsNullOrEmpty(space_sku))
                return null;

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();

                if (!IsValidSpace(conn, space_sku))
                    return null;

                MediaData? oneMediaData = null;
                Dictionary<string, List<MediaData>> spaceMediaData = new Dictionary<string, List<MediaData>>();

                List<MediaData> mediaData = new List<MediaData>();
                string sumQuery = string.Format("select * from tifoxr_media tm where sku = '{0}';", space_sku);
                var command = new SqlCommand(sumQuery, conn);

                int mediaType;
                string name, mediaLink;
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        name = reader.GetString(1);
                        mediaLink = reader.GetString(2);
                        mediaType = reader.GetInt32(3);

                        oneMediaData = new MediaData() { MediaType = mediaType, MediaLink = mediaLink, Name = name, Sku = space_sku };

                        mediaData.Add(oneMediaData);
                    }

                    spaceMediaData.Add(TifoXRSpace, mediaData);
                }

                conn.Close();

                return spaceMediaData;
            }
        }

        [HttpGet("GetWebPortalsData")]
        public Dictionary<string, List<WebPortalData>>? GetWebPortalsData(string space_sku)
        {
            if (string.IsNullOrEmpty(space_sku))
                return null;

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();

                if (!IsValidSpace(conn, space_sku))
                    return null;

                WebPortalData? webPortal = null;
                Dictionary<string, List<WebPortalData>> spaceWebPortals = new Dictionary<string, List<WebPortalData>>();

                List<WebPortalData> webPortals = new List<WebPortalData>();
                string sumQuery = string.Format("select * from tifoxr_web_portal twp where sku = '{0}';", space_sku);
                var command = new SqlCommand(sumQuery, conn);

                int mediaType;
                string? thumbnailLink = null;
                string name, portalLink, mediaLink, description;
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        name = reader.GetString(1);
                        portalLink = reader.GetString(2);
                        mediaLink = reader.GetString(3);
                        description = reader.GetString(4);
                        mediaType = reader.GetInt32(5);
                        if (mediaType == MediaVideo)
                            thumbnailLink = reader.GetString(6);

                        webPortal = new WebPortalData() { Description = description, MediaType = mediaType, MediaLink = mediaLink, Name = name, PortalLink = portalLink, ThumbnailLink = thumbnailLink, Sku = space_sku };

                        webPortals.Add(webPortal);
                    }

                    spaceWebPortals.Add(TifoXRSpace, webPortals);
                }

                conn.Close();

                return spaceWebPortals;
            }
        }
    }
}
