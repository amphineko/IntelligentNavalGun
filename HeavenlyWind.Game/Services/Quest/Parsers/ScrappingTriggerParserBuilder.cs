﻿using Sakuno.Collections;
using Sakuno.KanColle.Amatsukaze.Game.Services.Quest.Triggers;

namespace Sakuno.KanColle.Amatsukaze.Game.Services.Quest.Parsers
{
    [TriggerName("scrapping")]
    class ScrappingTriggerParserBuilder : TriggerParserBuilder
    {
        static ScrappingTrigger r_Any = new ScrappingTrigger(null);
        static HybridDictionary<int, Trigger> r_CachedFunctions = new HybridDictionary<int, Trigger>();

        public override Parser<Trigger> Parser { get; } =
            from rEquipmentID in ValueTypeOption(Number)
            select rEquipmentID == null ? r_Any : r_CachedFunctions.Get(rEquipmentID.Value, r => new ScrappingTrigger(r));
    }
}
