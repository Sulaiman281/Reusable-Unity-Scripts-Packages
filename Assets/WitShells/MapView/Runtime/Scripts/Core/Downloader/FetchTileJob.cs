using System.IO;
using System.Threading.Tasks;
using SQLite;
using UnityEngine;
using WitShells.ThreadingJob;

namespace WitShells.MapView
{
    public class FetchTileJob : ThreadJob<Tile>
    {
        public bool onlineDownload;
        public readonly string dbPath;
        private readonly string _url;
        private readonly Vector2Int _coordinate;
        private readonly int _zoomLevel;
        private readonly bool _showLabels;
        private readonly bool _cacheTile;

        public SQLiteConnection DbConnection
        {
            get
            {
                if (File.Exists(dbPath))
                {
                    return new SQLiteConnection(dbPath);
                }
                else
                {
                    throw new FileNotFoundException($"Database file not found at path: {dbPath}");
                }
            }
        }

        public FetchTileJob(string databasePath, Vector2Int coordinate, int zoomLevel, bool showLabels, bool onlineDownload = true, bool cacheTile = true)
        {
            dbPath = databasePath;
            _coordinate = coordinate;
            _zoomLevel = zoomLevel;
            _showLabels = showLabels;
            _cacheTile = cacheTile;
            this.onlineDownload = onlineDownload;

            _url = Utils.MakeTileUrl(showLabels, coordinate.x, coordinate.y, zoomLevel);
            IsAsync = true;
        }

        public FetchTileJob(string url)
        {
            _url = url;
            IsAsync = true;
        }

        public override async Task<Tile> ExecuteAsync()
        {
            try
            {
                // first check local db
                if (File.Exists(dbPath))
                {
                    using var connection = DbConnection;
                    var tile = MapTileManager.GetTile(_coordinate.x, _coordinate.y, _zoomLevel, connection);
                    if (tile != null)
                    {
                        return tile;
                    }
                }

                if (!onlineDownload)
                {
                    return null;
                }

                // then fetch online
                var data = await DownloadTile(_url);
                if (data == null || data.Length == 0)
                {
                    return null;
                }



                Tile newTile = new Tile
                {
                    TileX = _coordinate.x,
                    TileY = _coordinate.y,
                    Zoom = _zoomLevel,
                };

                if (_showLabels)
                {
                    newTile.GeoData = data;
                }
                else
                {
                    newTile.NormalData = data;
                }

                // cache to db
                if (_cacheTile)
                {
                    using var connection = DbConnection;
                    connection.Insert(newTile);
                }

                return newTile;
            }
            catch
            {
                throw;
            }
        }

        public static async Task<byte[]> DownloadTile(string url)
        {
            try
            {
                var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
                request.Accept = "image/webp,image/apng,image/*,*/*;q=0.8";
                request.Headers["Accept-Language"] = "en-US,en;q=0.9";
                request.Referer = "https://maps.google.com/";
                var response = await request.GetResponseAsync();
                using var stream = response.GetResponseStream() ?? throw new System.Exception("No response stream");
                using var ms = new System.IO.MemoryStream();
                await stream.CopyToAsync(ms);
                return ms.ToArray();
            }
            catch
            {
                throw;
            }
        }

        public override Tile Execute()
        {
            return null;
        }
    }
}