﻿using Sakuno.KanColle.Amatsukaze.Game.Models;
using Sakuno.KanColle.Amatsukaze.Game.Models.Battle;
using Sakuno.KanColle.Amatsukaze.Game.Parsers;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace Sakuno.KanColle.Amatsukaze.Game.Services.Records
{
    public class FateRecords : RecordsGroup
    {
        public override string GroupName => "fate";

        HashSet<Ship> r_SunkShips = new HashSet<Ship>();

        internal FateRecords(SQLiteConnection rpConnection) : base(rpConnection)
        {
            var rBattleResultApis = new[]
            {
                "api_req_sortie/battleresult",
                "api_req_combined_battle/battleresult",
            };
            DisposableObjects.Add(SessionService.Instance.Subscribe(rBattleResultApis, ProcessBattleResult));

            DisposableObjects.Add(SessionService.Instance.Subscribe("api_port/port", delegate
            {
                if (r_SunkShips.Count == 0)
                    return;

                var rAliveShips = r_SunkShips.Intersect(KanColleGame.Current.Port.Ships.Values);
                DeleteShipFate(rAliveShips);

                r_SunkShips.Clear();
            }));
        }

        protected override void CreateTable()
        {
            using (var rCommand = Connection.CreateCommand())
            {
                rCommand.CommandText =
                    "CREATE TABLE IF NOT EXISTS ship_fate(" +
                        "id INTEGER PRIMARY KEY NOT NULL, " +
                        "ship INTEGER NOT NULL, " +
                        "level INTEGER NOT NULL, " +
                        "time INTEGER NOT NULL, " +
                        "fate INTEGER NOT NULL);" +

                    "CREATE TABLE IF NOT EXISTS equipment_fate(" +
                        "id INTEGER PRIMARY KEY NOT NULL, " +
                        "equipment INTEGER NOT NULL, " +
                        "level INTEGER NOT NULL, " +
                        "proficiency INTEGER NOT NULL, " +
                        "time INTEGER NOT NULL, " +
                        "fate INTEGER NOT NULL);";

                rCommand.ExecuteNonQuery();
            }
        }

        internal void AddEquipmentFate(Equipment rpEquipment, Fate rpFate)
        {
            using (var rCommand = Connection.CreateCommand())
            {
                rCommand.CommandText = "INSERT OR IGNORE INTO equipment_fate(id, equipment, level, proficiency, time, fate) VALUES(@id, @equipment, @level, @proficiency, strftime('%s', 'now'), @fate);";
                rCommand.Parameters.AddWithValue("@id", rpEquipment.ID);
                rCommand.Parameters.AddWithValue("@equipment", rpEquipment.Info.ID);
                rCommand.Parameters.AddWithValue("@level", rpEquipment.Level == 0);
                rCommand.Parameters.AddWithValue("@proficiency", rpEquipment.Proficiency == 0);
                rCommand.Parameters.AddWithValue("@fate", (int)rpFate);

                rCommand.ExecuteNonQuery();
            }
        }
        internal void AddEquipmentFate(IEnumerable<Equipment> rpEquipment, Fate rpFate, ulong rpTimestamp = 0)
        {
            if (!rpEquipment.Any())
                return;

            using (var rCommand = Connection.CreateCommand())
            {
                rCommand.CommandText = "INSERT OR IGNORE INTO equipment_fate(id, equipment, level, proficiency, time, fate) VALUES" + rpEquipment.Select(r => $"({r.ID}, {r.Info.ID}, {r.Level}, {r.Proficiency}, @timestamp, @fate)").Join(", ") + ";";
                rCommand.Parameters.AddWithValue("@timestamp", rpTimestamp == 0 ? DateTimeOffset.Now.ToUnixTime() : rpTimestamp);
                rCommand.Parameters.AddWithValue("@fate", (int)rpFate);

                rCommand.ExecuteNonQuery();
            }
        }

        internal void AddShipFate(Ship rpShip, Fate rpFate, ulong rpTimestamp = 0)
        {
            using (var rTransaction = Connection.BeginTransaction())
            {
                var rTimestamp = rpTimestamp == 0 ? DateTimeOffset.Now.ToUnixTime() : rpTimestamp;

                using (var rCommand = Connection.CreateCommand())
                {
                    rCommand.CommandText = "INSERT OR IGNORE INTO ship_fate(id, ship, level, time, fate) VALUES(@id, @ship, @level, @timestamp, @fate);";
                    rCommand.Parameters.AddWithValue("@id", rpShip.ID);
                    rCommand.Parameters.AddWithValue("@ship", rpShip.Info.ID);
                    rCommand.Parameters.AddWithValue("@level", rpShip.Level);
                    rCommand.Parameters.AddWithValue("@timestamp", rTimestamp);
                    rCommand.Parameters.AddWithValue("@fate", (int)rpFate);

                    rCommand.ExecuteNonQuery();
                }

                if (rpShip.EquipedEquipment.Count > 0)
                    AddEquipmentFate(rpShip.EquipedEquipment, rpFate, rTimestamp);

                rTransaction.Commit();
            }
        }
        internal void AddShipFate(IEnumerable<Ship> rpShips, Fate rpFate, ulong rpTimestamp = 0)
        {
            if (!rpShips.Any())
                return;

            using (var rTransaction = Connection.BeginTransaction())
            {
                var rTimestamp = rpTimestamp == 0 ? DateTimeOffset.Now.ToUnixTime() : rpTimestamp;

                using (var rCommand = Connection.CreateCommand())
                {
                    rCommand.CommandText = "INSERT OR IGNORE INTO ship_fate(id, ship, level, time, fate) VALUES" + rpShips.Select(r => $"({r.ID}, {r.Info.ID}, {r.Level}, @timestamp, @fate)").Join(", ") + ";";
                    rCommand.Parameters.AddWithValue("@timestamp", rTimestamp);
                    rCommand.Parameters.AddWithValue("@fate", (int)rpFate);

                    rCommand.ExecuteNonQuery();
                }

                AddEquipmentFate(rpShips.SelectMany(r => r.EquipedEquipment), rpFate, rTimestamp);

                rTransaction.Commit();
            }
        }

        void DeleteShipFate(IEnumerable<Ship> rpShips)
        {
            if (!rpShips.Any())
                return;

            using (var rTransaction = Connection.BeginTransaction())
            {
                var rEquipmentIDs = rpShips.SelectMany(r => r.EquipedEquipment).Select(r => r.ID).ToArray();

                using (var rCommand = Connection.CreateCommand())
                {
                    rCommand.CommandText = $"DELETE FROM ship_fate WHERE id IN ({rpShips.Select(r => r.ID.ToString()).Join(", ")});";

                    if (rEquipmentIDs.Length > 0)
                        rCommand.CommandText += $"DELETE FROM equipment_fate WHERE id IN ({rEquipmentIDs.Select(r => r.ToString()).Join(", ")});";

                    rCommand.ExecuteNonQuery();
                }

                rTransaction.Commit();
            }
        }

        void ProcessBattleResult(ApiData rpData)
        {
            var rBattle = BattleInfo.Current;
            var rCurrentStage = rBattle.CurrentStage;

            var rSunkShips = rCurrentStage.Friend.Where(r => r.State == BattleParticipantState.Sunk).Select(r => ((FriendShip)r.Participant).Ship).Where(r_SunkShips.Add).ToArray();
            if (rSunkShips.Length == 0)
                return;

            AddShipFate(rSunkShips, Fate.Sunk, (ulong)rBattle.ID);
        }
    }
}
