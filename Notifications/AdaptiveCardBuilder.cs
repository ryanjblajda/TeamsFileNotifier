using Serilog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using TeamsFileNotifier.FileSystemMonitor;
using TeamsFileNotifier.Global;
using Microsoft.Graph.Models;
using AdaptiveCards;

namespace TeamsFileNotifier.Notifications
{
    public static class AdaptiveCardBuilder
    {
        public static JObject BuildAdaptiveCard(string title, string fileName, string fileUrl, string content, string iconurl, CustomActions actions)
        {
            JObject card = new JObject
            {
                ["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json",
                ["version"] = "1.6",
                ["type"] = "AdaptiveCard",
                ["msteams"] = new JObject
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
                                                ["text"] = ""
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            JObject payload = GenerateMessage(card);
            
            HandleCustomActions(actions, payload, card);

            SerializeContent(payload);

            return payload;
        }

        private static void SerializeContent(JObject card)
        {
            card["attachments"][0]["content"] = JsonConvert.SerializeObject(card["attachments"][0]["content"]);
        }

        private static void HandleCustomActions(CustomActions actions, JObject message, JObject card)
        {
            if (card != null)
            {
                if (actions.TagTeamMembers.Count != 0)
                {
                    Log.Information("adding tagged team members to post");
                    card["msteams"]["entities"] = GenerateTaggedTeamMembers(actions.TagTeamMembers);

                    if ((JArray)card["msteams"]["entities"] != null) {

                        var bodyArray = (JArray)card["body"];
                        bodyArray.Insert(bodyArray.Count - 1, GenerateMentionLineFromEntities((JArray)card["msteams"]["entities"]));
                    }
                }
                else { Log.Debug("no tagged team members to add"); }
            }
            else { Log.Debug("card null"); }
        }

        private static JArray GenerateTaggedTeamMembers(List<string> names)
        {
            var tagged = GetAllTaggedTeamMemberDetails(names, Values.AccessToken);
            return GenerateMentionEntities(tagged.Result);
        }

        private static JObject GenerateMessage(JObject adaptiveCard)
        {
            var payload = new JObject()
            {
                ["body"] = new JObject()
                {
                    ["contentType"] = "html",
                    ["content"] = "<attachment id=\"1\"></attachment>"
                },
                ["attachments"] = new JArray()
                {
                    new JObject()
                    {
                        ["id"] = "1",
                        ["contentType"] = "application/vnd.microsoft.card.adaptive",
                        ["contentUrl"] = null,
                        ["content"] = adaptiveCard
                    }
                }
            };

            return payload;
        }

        private static async Task<(string id, string name)> GetTaggedTeamMemberByEmail(string email, string accessToken)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            string requestUrl = $"https://graph.microsoft.com/v1.0/users?$filter=userPrincipalName eq '{email}' or mail eq '{email}'&$select=id,displayName";

            var response = await client.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode) { return ("", ""); }

            string content = await response.Content.ReadAsStringAsync();
            
            var json = JObject.Parse(content);

            var user = json["value"]?.FirstOrDefault();
            
            if (user == null) return ("", "");
            
            return (user["id"] == null ? "": user["id"].ToString(), user["displayName"] == null ? "" : user["displayName"].ToString());
        }

        private static string CheckUsername(string username)
        {
            string result = username.Trim();
            result = result.ReplaceLineEndings("");

            //if the username already has an email address added in there
            if (Regex.IsMatch(result, "^\\S+@\\S+\\.\\S+$")) { return result; }
            //if not add the email
            else { result = $"{username}{Values.EmailDomain}"; }

            Log.Debug($"AdaptiveCardBuilder | created email address for team member: {username}: {result}");

            return result;
        }

        private static async Task<List<(string id, string name)>> GetAllTaggedTeamMemberDetails(List<string> usernames, string token)
        {
            List<(string id, string name)> details = new List<(string id, string name)>();

            foreach (var user in usernames) {

                string check = CheckUsername(user);

                var result = await GetTaggedTeamMemberByEmail(check, token);
                if (result.id != "" && result.name != "") {
                    Log.Information($"AdaptiveCardBuilder | Adding {result.name} -> {result.id} to the tagged member list");
                    details.Add(result); 
                }               
            }

            return details;
        }

        public static JObject GenerateMentionLineFromEntities(JArray entities)
        {
            //extract the name of each team member to be mentioned
            var mentions = String.Join(" ", entities.Select(e => $"<at>{e["mentioned"]?["name"]?.ToString()}</at>").ToArray());
            
            //join the members and separate with the characters below
            // Create a new RichTextBlock with the mention line
            var mentionBlock = new JObject
            {
                ["type"] = "TextBlock",
                ["text"] = mentions,
                ["wrap"] = true
            };

            return mentionBlock;
        }

        private static JArray GenerateMentionEntities(List<(string id, string name)> mentions)
        {
            var entities = new JArray();

            mentions.ForEach(item =>
            {
                var (id, name) = item;
                var atTag = $"<at>{name}</at>";

                entities.Add(new JObject
                {
                    ["type"] = "mention",
                    ["text"] = atTag,
                    ["mentioned"] = new JObject
                    {
                        ["id"] = id,
                        ["name"] = name
                    }
                });
            });

            return entities;
        }
    }
}
