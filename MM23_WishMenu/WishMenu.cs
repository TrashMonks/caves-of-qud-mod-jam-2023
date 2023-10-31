// note from gnarf: I wrote this in like 1 hour at midnight, I'm not proud of it
// open to PR's to clean up syntax/code organization of this little tool

using ConsoleLib.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using XRL;
using XRL.World;
using XRL.UI;
using XRL.Wish;

namespace XRL.World.Parts
{
    [HasWishCommand]
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    public class MM23_Wish_Handler : IPlayerPart
    {
        public string COMMAND_NAME = "MM23_Wish_Menu";

        public class WishCommandXML
        {
            public string DisplayName = "default";
            public List<string> Commands;
            public string Author;
            public string ModName;
            public IRenderable Renderable;
            public string DisplayText
            {
                get
                {
                    string result = $"{DisplayName}\n";
                    if (!(string.IsNullOrEmpty(Author) || string.IsNullOrEmpty(ModName)))
                    {
                        result =
                            $"{result}{Markup.Color("c", $"- From \"{ModName}\" by {Author}")}\n";
                    }

                    foreach (var command in Commands)
                        result = $"{result}{Markup.Color("K", $"- wish: {command}")}\n";
                    return result;
                }
            }
        }

        // this is for stable compat - replace with Renderable.UITile after 206 is stable
        private static Renderable UITile(
            string tilePath,
            char foregroundColorCode = 'y',
            char detailColorCode = 'w',
            string noTileAlt = " ",
            char noTileColor = '\0'
        )
        {
            char useColorChar = noTileColor != '\0' ? noTileColor : foregroundColorCode;
            string noColorCode = $"&{useColorChar}";

            return new Renderable(
                tilePath,
                noTileAlt,
                noColorCode,
                "&" + foregroundColorCode,
                detailColorCode
            );
        }

        [GameBasedCacheInit]
        [ModSensitiveCacheInit]
        public static void CacheInit()
        {
            MenuItems = new List<WishCommandXML>();
            foreach (var stream in DataManager.YieldXMLStreamsWithRoot("wishcommands"))
            {
                stream.HandleNodes(nodes);
            }
        }

        public static void HandleNodes(XmlDataHelper xml) => xml.HandleNodes(nodes);

        public static Dictionary<string, Action<XmlDataHelper>> nodes = new Dictionary<
            string,
            Action<XmlDataHelper>
        >
        {
            { "wishcommands", HandleNodes },
            { "wish", HandleWishNode },
        };

        // also for 204.x compat - remove in 206.x there is default
        private static List<string> parseCSV(string s) => s.Split(',').ToList();

        public static void HandleWishNode(XmlDataHelper xml)
        {
            WishCommandXML data = new WishCommandXML();
            data.Renderable = UITile("empty", 'k', 'k');
            data.Commands = xml.ParseAttribute(
                "Commands",
                data.Commands,
                required: true,
                parse: parseCSV
            );
            var firstCommand = data.Commands[0];
            if (firstCommand.StartsWith("item:"))
                firstCommand = firstCommand.Substring(5);
            GameObject sample = null;
            try
            {
                sample = GameObject.createSample(firstCommand);
                data.DisplayName = $"Spawn {sample.DisplayName}";
                data.Renderable = new Renderable(sample.RenderForUI());
            }
            catch (Exception e)
            {
                // xml.HandleException(e);
            }
            data.DisplayName = xml.ParseAttribute("DisplayName", data.DisplayName);
            data.Author = xml.ParseAttribute("Author", data.Author, required: true);
            data.ModName = xml.ParseAttribute("ModName", data.ModName, required: true);
            var icon = xml.ParseAttribute<string>("Icon", null);
            if (!string.IsNullOrEmpty(icon))
            {
                var colors = xml.ParseAttribute<string>("ColorPair", "yw");
                data.Renderable = UITile(icon, colors[0], colors[1]);
            }
            // if (string.IsNullOrEmpty(data.DisplayName))
            // {

            // }
            MenuItems.Add(data);
            xml.DoneWithElement();
        }

        [ModSensitiveStaticCache]
        public static List<WishCommandXML> MenuItems = new List<WishCommandXML>();

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade) || ID == CommandEvent.ID;
        }

        [WishCommand("mashup")]
        public static void MashupWish()
        {
            var choice = Popup.ShowOptionList(
                "Monster Mash-Up Wish Menu",
                MenuItems.Select(a => a.DisplayText).ToArray(),
                AllowEscape: true,
                Icons: MenuItems.Select(a => a.Renderable).ToArray()
            );
            if (choice >= 0)
            {
                foreach (var command in MenuItems[choice].Commands)
                {
                    XRL.World.Capabilities.Wishing.HandleWish(The.Player, command);
                }
            }
        }

        public override bool HandleEvent(CommandEvent E)
        {
            if (MenuItems == null || MenuItems.Count == 0)
                CacheInit();
            if (E.Command == COMMAND_NAME) { }
            return base.HandleEvent(E);
        }
    }
}

[PlayerMutator]
public class MyPlayerMutator : IPlayerMutator
{
    public void mutate(GameObject player)
    {
        // modify the player object when a New Game begins
        // for example, add a custom part to the player:
        player.AddPart<XRL.World.Parts.MM23_Wish_Handler>();
    }
}
