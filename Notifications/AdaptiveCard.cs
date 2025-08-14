using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamsFileNotifier.Notifications
{
    public static class AdaptiveCardBuilder
    {
        public static JObject BuildFileUpdatedCard(string title, string fileName, string fileUrl, string content, string iconurl)
        {
            JObject card = new JObject
            {
                ["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json",
                ["version"] = "1.5",
                ["type"] = "AdaptiveCard",
                ["msTeams"] = new JObject
                {
                    ["width"] = "full"
                },
                ["body"] = new JArray
            {
                // First ColumnSet (logo + heading)
                new JObject
                {
                    ["type"] = "ColumnSet",
                    ["columns"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "Column",
                            ["width"] = "stretch",
                            ["spacing"] = "Medium",
                            ["items"] = new JArray
                            {
                                new JObject
                                {
                                    ["type"] = "Image",
                                    ["url"] = iconurl,
                                    ["height"] = "30px"
                                }
                            },
                            ["height"] = "stretch",
                            ["verticalContentAlignment"] = "Center"
                        },
                        new JObject
                        {
                            ["type"] = "Column",
                            ["width"] = "stretch",
                            ["items"] = new JArray
                            {
                                new JObject
                                {
                                    ["type"] = "TextBlock",
                                    ["text"] = title,
                                    ["wrap"] = true,
                                    ["height"] = "stretch",
                                    ["style"] = "heading",
                                    ["size"] = "ExtraLarge"
                                }
                            }
                        }
                    }
                },
                // Second ColumnSet (File Name label and value)
                new JObject
                {
                    ["type"] = "ColumnSet",
                    ["columns"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "Column",
                            ["width"] = "auto",
                            ["items"] = new JArray
                            {
                                new JObject
                                {
                                    ["type"] = "TextBlock",
                                    ["text"] = "File Name:",
                                    ["wrap"] = true,
                                    ["style"] = "heading",
                                    ["separator"] = true,
                                    ["size"] = "Large"
                                }
                            }
                        },
                        new JObject
                        {
                            ["type"] = "Column",
                            ["width"] = "stretch",
                            ["items"] = new JArray
                            {
                                new JObject
                                {
                                    ["type"] = "TextBlock",
                                    ["text"] = fileName,
                                    ["wrap"] = true,
                                    ["height"] = "stretch",
                                    ["weight"] = "Lighter",
                                    ["size"] = "Large"
                                }
                            }
                        }
                    }
                },
                // Third ColumnSet (IP Table header)
                new JObject
                {
                    ["type"] = "ColumnSet",
                    ["columns"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "Column",
                            ["width"] = "stretch",
                            ["items"] = new JArray
                            {
                                new JObject
                                {
                                    ["type"] = "TextBlock",
                                    ["text"] = "Details",
                                    ["wrap"] = true,
                                    ["horizontalAlignment"] = "Left",
                                    ["spacing"] = "None",
                                    ["height"] = "stretch",
                                    ["style"] = "columnHeader",
                                    ["size"] = "Large",
                                    ["separator"] = true
                                }
                            }
                        }
                    }
                },
                // Fourth ColumnSet (RichTextBlock with IP list)
                new JObject
                {
                    ["type"] = "ColumnSet",
                    ["columns"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "Column",
                            ["width"] = "stretch",
                            ["items"] = new JArray
                            {
                                new JObject
                                {
                                    ["type"] = "RichTextBlock",
                                    ["separator"] = true,
                                    ["inlines"] = new JArray
                                    {
                                        new JObject
                                        {
                                            ["type"] = "TextRun",
                                            ["text"] = content
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            };


            return card;
        }
    }
}
