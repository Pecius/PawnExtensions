using System;
using System.Collections.Generic;
using System.Xml;
using Verse;

namespace PawnExtensions
{
    public class BackCompatibilityDef : Def
    {
        private Backstory backstory;

        [Unsaved]
        private Dictionary<Type, Dictionary<string, string>> defReplacements = new Dictionary<Type, Dictionary<string, string>>();

        private string modID;
        public string ModID => modID;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            foreach (XmlNode node in xmlRoot.ChildNodes)
            {
                if (node is XmlComment)
                    continue;

                string nodeName = node.Name;

                switch (nodeName)
                {
                    case "defName":
                        defName = node.FirstChild.Value;
                        break;

                    case "modID":
                        modID = node.FirstChild.Value;
                        break;

                    case "backstory":
                        backstory = DirectXmlToObject.ObjectFromXml<Backstory>(node, false);
                        break;

                    default:
                        Type type = GenTypes.GetTypeInAnyAssembly(nodeName);

                        if (type == null)
                        {
                            Log.Error($"Invalid type {nodeName}");
                            continue;
                        }

                        ProcessDefReplacers(node.ChildNodes, type);
                        break;
                }
            }
        }

        public override void PostLoad()
        {
            if (modID == null || !ModsConfig.IsActive(modID))
            {
                Log.Error($"modID '{modID.ToStringSafe()}' isn't valid, {defName} won't be registered!");
                return;
            }

            ScribeCompatUtility.RegisterBackCompatibilityDef(this);
        }

        public bool ShouldRefreshBackstory(string backstoryName)
        {
            var updates = backstory?.update;
            if (updates == null)
                return false;

            return updates.Contains(backstoryName);
        }

        public bool TryGetBackstoryOpposingReplacement(string backstoryName, out string destination)
        {
            var replacements = backstory?.replaceOpposing;

            if (replacements == null)
            {
                destination = null;
                return false;
            }

            return replacements.TryGetValue(backstoryName, out destination);
        }

        public bool TryGetBackstoryReplacement(string backstoryName, out string destination)
        {
            var replacements = backstory?.replaceMissing;

            if (replacements == null)
            {
                destination = null;
                return false;
            }

            return replacements.TryGetValue(backstoryName, out destination);
        }

        public bool TryGetDefReplacement(Type defType, string defName, out string destination)
        {
            if (defReplacements.TryGetValue(defType, out var database))
            {
                if (database.TryGetValue(defName, out string replacement))
                {
                    destination = replacement;
                    return true;
                }
            }

            destination = null;
            return false;
        }

        private static void ProcessKeyValues(XmlNodeList nodes, Dictionary<string, string> destination)
        {
            foreach (XmlNode node in nodes)
            {
                if (node is XmlComment)
                    continue;

                destination[node.Name] = node.FirstChild.Value;
            }
        }

        private void ProcessDefReplacers(XmlNodeList nodes, Type type)
        {
            if (!defReplacements.TryGetValue(type, out var database))
            {
                database = new Dictionary<string, string>();
                defReplacements[type] = database;
            }

            ProcessKeyValues(nodes, database);
        }

        private class Backstory
        {
            public Dictionary<string, string> replaceMissing;
            public Dictionary<string, string> replaceOpposing;
            public List<string> update;

            public void LoadDataFromXmlCustom(XmlNode xmlRoot)
            {
                foreach (XmlNode node in xmlRoot.ChildNodes)
                {
                    if (node is XmlComment)
                        continue;

                    string nodeName = node.Name;

                    switch (nodeName)
                    {
                        case "update":
                            update = DirectXmlToObject.ObjectFromXml<List<string>>(node, false);
                            break;

                        case "replaceOpposing":
                            replaceOpposing = new Dictionary<string, string>();
                            ProcessKeyValues(node.ChildNodes, replaceOpposing);
                            break;

                        case "replaceMissing":
                            replaceMissing = new Dictionary<string, string>();
                            ProcessKeyValues(node.ChildNodes, replaceMissing);
                            break;

                        default:
                            Log.Error($"Invalid node name {nodeName}");
                            break;
                    }
                }
            }
        }
    }
}