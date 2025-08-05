using Life;
using Life.AirportSystem;
using Life.DB;
using Life.InventorySystem;
using Life.Network;
using Life.VehicleSystem;
using SQLite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions.Must;

namespace Mileage581
{
    public class Main : Plugin
    {
        public string directoryPath;
        public string configPath;
        public Config config;
        public Dictionary<Vehicle, Vector3> lastPositions = new Dictionary<Vehicle, Vector3>();
        public string dbPath;
        public SQLiteAsyncConnection db { get; set; }

        public Main(IGameAPI api) : base(api) { }

        public override async void OnPluginInit()
        {
            base.OnPluginInit();
            directoryPath = Path.Combine(pluginsPath, Assembly.GetExecutingAssembly().GetName().Name);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            configPath = Path.Combine(directoryPath, "config.json");
            if (!File.Exists(configPath))
            {
                config = new Config();
                File.WriteAllText(configPath, Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented));
            }
            else
            {
                config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
            }
            dbPath = Path.Combine(directoryPath, "database.sqlite");
            if (!Directory.Exists(Path.GetDirectoryName(dbPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath));
            }
            db = new SQLiteAsyncConnection(dbPath);
            await db.CreateTableAsync<Mileage>();
            Nova.man.StartCoroutine(Loop());
            new SChatCommand("/mileage", new string[] { "/kilometrage", "/km" }, "Affiche le kilometrage du véhicule", "/mileage", async (player, args) =>
            {
                if (player.GetVehicleId() > 0)
                {
                    var query = await db.Table<Mileage>().Where(obj => obj.VehicleId == player.setup.driver.vehicle.VehicleDbId).FirstOrDefaultAsync();
                    if (query != null)
                    {
                        player.SendText($"<color={LifeServer.COLOR_BLUE}>Votre véhicule à {query.Value:F2}km.</color>");
                    }
                    else
                    {
                        player.SendText($"<color={LifeServer.COLOR_BLUE}>Votre véhicule à 0km.</color>");
                    }
                }
                else
                    player.Notify("Mileage581", "Vous n'êtes pas dans un véhicule.", NotificationManager.Type.Error);
            }).Register();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{Assembly.GetExecutingAssembly().GetName().Name} initialise !");
            Console.ResetColor();
        }

        public IEnumerator Loop()
        {
            while (true)
            {
                UpdateMileage();
                yield return new UnityEngine.WaitForSeconds(3);
            }
        }

        public async void UpdateMileage()
        {
            foreach (var player in Nova.server.Players.Where(obj => obj.isSpawned).ToList())
            {
                try
                {
                    if (player.GetVehicleId() > 0)
                    {
                        var vehicle = player.setup.driver.vehicle;
                        var lifeVehicle = Nova.v.GetVehicle(vehicle.VehicleDbId);
                        if (lifeVehicle == null)
                        {
                            continue;
                        }
                        var currentPosition = vehicle.transform.position;
                        var query = await db.Table<Mileage>().Where(obj => obj.VehicleId == lifeVehicle.vehicleId).FirstOrDefaultAsync();
                        if (query == null)
                        {
                            var instance = new Mileage()
                            {
                                VehicleId = lifeVehicle.vehicleId,
                                Value = 0
                            };
                            await db.InsertAsync(instance);
                            lastPositions[vehicle] = currentPosition;
                        }
                        else
                        {
                            if (lastPositions.TryGetValue(vehicle, out var lastPosition))
                            {
                                var newValue = query.Value + Vector3.Distance(lastPosition, currentPosition) / 1000f;
                                query.Value = newValue * config.multiplier;
                                await db.UpdateAsync(query);
                            }
                            else
                            {
                                lastPositions[vehicle] = currentPosition;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Une erreur est survenue lors de la mise a jour du kilométrage de {player.FullName}");
                    Console.WriteLine(ex.ToString());
                    Console.ResetColor();
                }
            }
        }
    }
}
