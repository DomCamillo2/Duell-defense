using System;

namespace DefenseDuell.Core
{
    /// <summary>Six free-placement maps with shared terrain blockers per field and stable catalog IDs.</summary>
    public static class MapCatalog
    {
        public const int Count = 6;

        // The legacy position list is retained for compatibility. Free-placement maps use
        // actual coordinates for both combat towers and mines; these are not mandatory pads.

        public static MapDefinition Create(int index)
        {
            switch (index)
            {
                case 0:
                    return new MapDefinition(new[]
                    {
                        new MapPoint(30, 230), new MapPoint(480, 230), new MapPoint(610, 180),
                        new MapPoint(780, 180), new MapPoint(900, 280), new MapPoint(1010, 300),
                        new MapPoint(1320, 300), new MapPoint(1420, 400), new MapPoint(1420, 500),
                        new MapPoint(1320, 600), new MapPoint(960, 600), new MapPoint(820, 510),
                        new MapPoint(600, 510), new MapPoint(460, 650), new MapPoint(220, 650),
                        new MapPoint(130, 740), new MapPoint(220, 830), new MapPoint(560, 830),
                        new MapPoint(760, 760), new MapPoint(1010, 800), new MapPoint(1270, 800),
                        new MapPoint(1440, 740), new MapPoint(1570, 740)
                    }, new[]
                    {
                        new MapPoint(80, 130), new MapPoint(180, 330), new MapPoint(320, 130),
                        new MapPoint(460, 130), new MapPoint(600, 90), new MapPoint(760, 90),
                        new MapPoint(820, 330), new MapPoint(1000, 390), new MapPoint(1160, 210),
                        new MapPoint(1300, 210), new MapPoint(1480, 330), new MapPoint(1460, 590),
                        new MapPoint(1280, 510), new MapPoint(1140, 510), new MapPoint(1000, 690),
                        new MapPoint(820, 610), new MapPoint(720, 430), new MapPoint(520, 450),
                        new MapPoint(540, 690), new MapPoint(340, 550), new MapPoint(140, 590),
                        new MapPoint(100, 830), new MapPoint(280, 750), new MapPoint(640, 890),
                        new MapPoint(680, 690), new MapPoint(780, 850), new MapPoint(920, 870),
                        new MapPoint(1060, 890), new MapPoint(1140, 710), new MapPoint(1320, 890),
                        new MapPoint(1460, 830), new MapPoint(1340, 690)
                    }, "meadow", "Grünes Tal", 0, width: 1600, height: 960, mineSlots: Array.Empty<int>(),
                    freePlacement: true, blockedAreas: new[]
                    {
                        new MapRectangle(910, 30, 1110, 190),
                        new MapRectangle(1230, 30, 1430, 190),
                        new MapRectangle(30, 310, 230, 470),
                        new MapRectangle(190, 30, 310, 150),
                        new MapRectangle(430, 30, 590, 110),
                        new MapRectangle(590, 270, 750, 430),
                        new MapRectangle(350, 310, 470, 470),
                        new MapRectangle(630, 590, 790, 670),
                        new MapRectangle(1150, 390, 1270, 510)
                    });
                case 1:
                    return new MapDefinition(new[]
                    {
                        new MapPoint(30, 780), new MapPoint(180, 780), new MapPoint(270, 680),
                        new MapPoint(270, 380), new MapPoint(390, 240), new MapPoint(650, 240),
                        new MapPoint(790, 120), new MapPoint(1050, 120), new MapPoint(1200, 260),
                        new MapPoint(1200, 490), new MapPoint(1070, 610), new MapPoint(830, 610),
                        new MapPoint(690, 460), new MapPoint(500, 460), new MapPoint(460, 600),
                        new MapPoint(600, 780), new MapPoint(930, 840), new MapPoint(1210, 810),
                        new MapPoint(1350, 680), new MapPoint(1570, 680)
                    }, new[]
                    {
                        new MapPoint(80, 690), new MapPoint(160, 870), new MapPoint(320, 750),
                        new MapPoint(180, 570), new MapPoint(180, 430), new MapPoint(260, 250),
                        new MapPoint(420, 150), new MapPoint(560, 150), new MapPoint(740, 290),
                        new MapPoint(720, 70), new MapPoint(900, 210), new MapPoint(1120, 70),
                        new MapPoint(1080, 270), new MapPoint(1300, 310), new MapPoint(1300, 450),
                        new MapPoint(1200, 610), new MapPoint(1020, 510), new MapPoint(900, 710),
                        new MapPoint(720, 630), new MapPoint(780, 430), new MapPoint(560, 370),
                        new MapPoint(560, 550), new MapPoint(460, 730), new MapPoint(580, 870),
                        new MapPoint(720, 890), new MapPoint(1040, 730), new MapPoint(1200, 890),
                        new MapPoint(1340, 850), new MapPoint(1340, 590), new MapPoint(1460, 770),
                        new MapPoint(1480, 590), new MapPoint(380, 590)
                    }, "desert", "Sonnenpass", 1, width: 1600, height: 960, mineSlots: Array.Empty<int>(),
                    freePlacement: true, blockedAreas: new[]
                    {
                        new MapRectangle(30, 30, 230, 190),
                        new MapRectangle(1270, 30, 1470, 190),
                        new MapRectangle(1350, 310, 1550, 470),
                        new MapRectangle(30, 310, 190, 470),
                        new MapRectangle(310, 750, 470, 910),
                        new MapRectangle(350, 30, 470, 150),
                        new MapRectangle(790, 230, 990, 390),
                        new MapRectangle(30, 590, 110, 710),
                        new MapRectangle(1470, 750, 1550, 870),
                        new MapRectangle(430, 310, 590, 390)
                    });
                case 2:
                    return new MapDefinition(new[]
                    {
                        new MapPoint(30, 160), new MapPoint(350, 160), new MapPoint(470, 260),
                        new MapPoint(700, 260), new MapPoint(830, 150), new MapPoint(1140, 150),
                        new MapPoint(1290, 280), new MapPoint(1290, 430), new MapPoint(1160, 540),
                        new MapPoint(900, 540), new MapPoint(780, 440), new MapPoint(560, 440),
                        new MapPoint(400, 570), new MapPoint(400, 750), new MapPoint(550, 850),
                        new MapPoint(820, 850), new MapPoint(980, 740), new MapPoint(1140, 740),
                        new MapPoint(1280, 840), new MapPoint(1460, 840), new MapPoint(1570, 730)
                    }, new[]
                    {
                        new MapPoint(80, 70), new MapPoint(160, 250), new MapPoint(300, 70),
                        new MapPoint(460, 130), new MapPoint(520, 350), new MapPoint(640, 170),
                        new MapPoint(820, 290), new MapPoint(860, 70), new MapPoint(1000, 250),
                        new MapPoint(1180, 70), new MapPoint(1160, 290), new MapPoint(1380, 330),
                        new MapPoint(1320, 510), new MapPoint(1220, 610), new MapPoint(1040, 450),
                        new MapPoint(880, 630), new MapPoint(760, 530), new MapPoint(620, 530),
                        new MapPoint(420, 450), new MapPoint(500, 610), new MapPoint(300, 630),
                        new MapPoint(360, 830), new MapPoint(560, 750), new MapPoint(700, 750),
                        new MapPoint(900, 890), new MapPoint(1020, 650), new MapPoint(1040, 830),
                        new MapPoint(1180, 870), new MapPoint(1300, 730), new MapPoint(1440, 730),
                        new MapPoint(900, 410), new MapPoint(1360, 190)
                    }, "frost", "Frosthafen", 2, width: 1600, height: 960, mineSlots: Array.Empty<int>(),
                    freePlacement: true, blockedAreas: new[]
                    {
                        new MapRectangle(30, 230, 230, 390),
                        new MapRectangle(30, 510, 230, 670),
                        new MapRectangle(1390, 310, 1550, 470),
                        new MapRectangle(510, 30, 630, 190),
                        new MapRectangle(30, 790, 150, 910),
                        new MapRectangle(550, 590, 750, 750),
                        new MapRectangle(870, 230, 1070, 390),
                        new MapRectangle(1470, 30, 1550, 150),
                        new MapRectangle(1350, 590, 1510, 670)
                    });
                case 3: return CreateEmeraldBasin();
                case 4: return CreateAmberCanyon();
                case 5: return CreateAuroraBay();
                default: throw new ArgumentOutOfRangeException(nameof(index), "Map index must be between 0 and 5.");
            }
        }

        // Individually authored terrain compositions. Rounded simulation travel lengths
        // remain 4532 / 4122 / 4038, exactly matching the previous large-map routes.
        // Every landscape mass is an explicit shared placement blocker, not hidden art collision.
        private static MapDefinition CreateEmeraldBasin()
            => new MapDefinition(new[]
            {
                new MapPoint(30, 240),
                new MapPoint(330, 240),
                new MapPoint(510, 420),
                new MapPoint(510, 1040),
                new MapPoint(730, 1260),
                new MapPoint(1450, 1260),
                new MapPoint(1670, 1040),
                new MapPoint(1670, 480),
                new MapPoint(1870, 280),
                new MapPoint(2160, 280),
                new MapPoint(2370, 490),
                new MapPoint(2370, 1075)
            }, new[] { new MapPoint(30, 500) }, "emerald-basin", "Smaragdkessel", 0,
                width: 2400, height: 1440, mineSlots: Array.Empty<int>(), freePlacement: true, blockedAreas: new[]
                {
                    new MapRectangle(80, 40, 280, 130),
                    new MapRectangle(760, 60, 1240, 220),
                    new MapRectangle(1810, 1170, 2260, 1380),
                    new MapRectangle(100, 580, 300, 1050),
                    new MapRectangle(850, 500, 1400, 1000),
                    new MapRectangle(1840, 560, 2160, 950),
                    new MapRectangle(70, 1260, 400, 1400),
                    new MapRectangle(1370, 40, 1600, 300)
                });

        private static MapDefinition CreateAmberCanyon()
            => new MapDefinition(new[]
            {
                new MapPoint(30, 1100),
                new MapPoint(360, 1100),
                new MapPoint(600, 920),
                new MapPoint(600, 350),
                new MapPoint(840, 170),
                new MapPoint(1260, 170),
                new MapPoint(1500, 350),
                new MapPoint(1500, 1000),
                new MapPoint(1740, 1180),
                new MapPoint(2070, 1180),
                new MapPoint(2370, 880),
                new MapPoint(2370, 682)
            }, new[] { new MapPoint(30, 500) }, "amber-canyon", "Bernsteinschlucht", 1,
                width: 2400, height: 1440, mineSlots: Array.Empty<int>(), freePlacement: true, blockedAreas: new[]
                {
                    new MapRectangle(100, 100, 400, 660),
                    new MapRectangle(1660, 50, 2270, 300),
                    new MapRectangle(70, 1230, 570, 1400),
                    new MapRectangle(1660, 1300, 2280, 1400),
                    new MapRectangle(850, 440, 1230, 1040),
                    new MapRectangle(1760, 480, 2160, 820),
                    new MapRectangle(180, 840, 370, 960)
                });

        private static MapDefinition CreateAuroraBay()
            => new MapDefinition(new[]
            {
                new MapPoint(30, 1180),
                new MapPoint(500, 1180),
                new MapPoint(740, 1000),
                new MapPoint(1060, 1000),
                new MapPoint(1300, 820),
                new MapPoint(1300, 520),
                new MapPoint(1060, 340),
                new MapPoint(1060, 160),
                new MapPoint(1740, 160),
                new MapPoint(1980, 340),
                new MapPoint(1980, 780),
                new MapPoint(2370, 780),
                new MapPoint(2370, 838)
            }, new[] { new MapPoint(30, 500) }, "aurora-bay", "Nordlichtbucht", 2,
                width: 2400, height: 1440, mineSlots: Array.Empty<int>(), freePlacement: true, blockedAreas: new[]
                {
                    new MapRectangle(80, 1280, 450, 1410),
                    new MapRectangle(100, 70, 800, 360),
                    new MapRectangle(1690, 1040, 2220, 1360),
                    new MapRectangle(2120, 60, 2320, 550),
                    new MapRectangle(1480, 380, 1790, 760),
                    new MapRectangle(100, 600, 440, 1000),
                    new MapRectangle(760, 520, 1040, 810)
                });

        public static GameContent[] CreateContents(GameContent template)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            template.Validate();
            TowerDefinition[] towers = template.Towers;
            UnitDefinition[] units = template.Units;
            var basis = template.BaseContent;
            var contents = new GameContent[Count];
            bool knownTemplate=false;
            for(int i=0;i<Count;i++)if(Create(i).Id==basis.Map.Id){knownTemplate=true;break;}
            for (int index = 0; index < Count; index++)
            {
                var chosenMap = Create(index);
                // Preserve authored geometry for the template's own canonical map without
                // moving it into slot zero when the currently selected map is a later entry.
                if (basis.Map.Id == chosenMap.Id || index==0 && !knownTemplate) chosenMap = basis.Map;
                contents[index] = new GameContent(template.Rules, towers, units, chosenMap, basis.Waves, template.Research, template.WaveRules)
                    .WithMatchRules(template.MatchRules.WithMap(chosenMap.Id));
            }
            return contents;
        }
    }
}
