﻿using System.Collections.Generic;

namespace Sakuno.KanColle.Amatsukaze
{
    public class ExtraStringResources : ModelBase
    {
        public Dictionary<int, string> Ships { get; internal set; }
        public Dictionary<int, string> ShipTypes { get; internal set; }
        public Dictionary<int, string> Equipment { get; internal set; }
        public Dictionary<int, string> Items { get; internal set; }
        public Dictionary<int, string> Expeditions { get; internal set; }
        public Dictionary<int, string> Quests { get; internal set; }
        public Dictionary<int, string> Areas { get; internal set; }
        public Dictionary<int, string> Maps { get; internal set; }

        internal ExtraStringResources() { }

        string GetName(Dictionary<int, string> rpDictionary, int rpID)
        {
            string rResult = null;
            rpDictionary?.TryGetValue(rpID, out rResult);
            return rResult;
        }

        public string GetShipName(int rpID) => GetName(Ships, rpID);
        public string GetShipTypeName(int rpID) => GetName(ShipTypes, rpID);
        public string GetEquipmentName(int rpID) => GetName(Equipment, rpID);
        public string GetItemName(int rpID) => GetName(Items, rpID);
        public string GetExpeditionName(int rpID) => GetName(Expeditions, rpID);
        public string GetQuestName(int rpID) => GetName(Quests, rpID);
        public string GetAreaName(int rpID) => GetName(Areas, rpID);
        public string GetMapName(int rpID) => GetName(Maps, rpID);
    }
}
