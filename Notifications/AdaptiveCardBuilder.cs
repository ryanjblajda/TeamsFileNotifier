using Serilog;
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
        public static string BuildAdaptiveCard(string title, string fileName, string fileUrl, string content, string iconurl, CustomActions actions, ChatMessage chat)
        {
            AdaptiveCard card = GenerateAdaptiveCardTemplate(title, fileName, fileUrl, content, iconurl);

            HandleCustomActions(card, chat, actions);

            return card.ToJson();
        }

        private static AdaptiveCard GenerateAdaptiveCardTemplate(string title, string file, string fileurl, string content, string icon)
        {
            // Build the card
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 6))
            {
                // Teams-specific properties
                AdditionalProperties = new SerializableDictionary<string, object>
                {
                    ["msteams"] = new Dictionary<string, object>
                    {
                        ["width"] = "full",
                        ["entities"] = new List<object>()
                    }
                }
            };

            // === First row: Logo + Title ===
            card.Body.Add(new AdaptiveColumnSet
            {
                Columns = new List<AdaptiveColumn>
                {
                    new AdaptiveColumn
                    {
                        Width = "stretch",
                        Spacing = AdaptiveSpacing.Medium,
                        VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center,
                        Items = new List<AdaptiveElement>
                        {
                            new AdaptiveImage(icon)
                            {
                                Height = AdaptiveHeight.Auto,
                                PixelHeight = 30
                            }
                        }
                    },
                    new AdaptiveColumn
                    {
                        Width = "stretch",
                        Items = new List<AdaptiveElement>
                        {
                            new AdaptiveTextBlock($"{Path.GetExtension(file).ToUpper()} File Updated")
                            {
                                Wrap = true,
                                Size = AdaptiveTextSize.ExtraLarge,
                                Style = AdaptiveTextBlockStyle.Heading
                            }
                        }
                    }
                }
            });

            // === Second row: File Name ===
            card.Body.Add(new AdaptiveColumnSet
            {
                Columns = new List<AdaptiveColumn>
                {
                    new AdaptiveColumn
                    {
                        Width = "auto",
                        Items = new List<AdaptiveElement>
                        {
                            new AdaptiveTextBlock("File Name:")
                            {
                                Wrap = true,
                                Style = AdaptiveTextBlockStyle.Heading,
                                Size = AdaptiveTextSize.Large,
                                Separator = true
                            }
                        }
                    },
                    new AdaptiveColumn
                    {
                        Width = "stretch",
                        Items = new List<AdaptiveElement>
                        {
                            new AdaptiveTextBlock(file)
                            {
                                Wrap = true,
                                Size = AdaptiveTextSize.Large,
                                Weight = AdaptiveTextWeight.Lighter
                            }
                        }
                    }
                }
            });

            // === Third row: "Details" header ===
            card.Body.Add(new AdaptiveColumnSet
            {
                Columns = new List<AdaptiveColumn>
                {
                    new AdaptiveColumn
                    {
                        Width = "stretch",
                        Items = new List<AdaptiveElement>
                        {
                            new AdaptiveTextBlock("Details")
                            {
                                Wrap = true,
                                Style = AdaptiveTextBlockStyle.Heading, // custom style
                                Size = AdaptiveTextSize.Large,
                                Separator = true
                            }
                        }
                    }
                }
            });

            // === Last row: Empty RichTextBlock ===
            card.Body.Add(new AdaptiveColumnSet
            {
                Columns = new List<AdaptiveColumn>
                {
                    new AdaptiveColumn
                    {
                        Width = "stretch",
                        Items = new List<AdaptiveElement>
                        {
                            new AdaptiveRichTextBlock
                            {
                                Separator = true,
                                Inlines = new List<AdaptiveInline>
                                {
                                    new AdaptiveTextRun(content) // empty run
                                }
                            }
                        }
                    }
                }
            });

            return card;
        }

        private static void HandleCustomActions(AdaptiveCard card, ChatMessage chat, CustomActions actions)
        {
            if (card != null)
            {
                if (actions.TagTeamMembers.Count != 0)
                {
                    Log.Information("AdaptiveCardBuilder | adding tagged team members to post");
                    var entities = GetAllTaggedTeamMemberDetails(actions.TagTeamMembers, Values.AccessToken);
                    chat.Mentions = GenerateAllEntitiesMentionList(entities.Result);
                    ((Dictionary<string, object>)card.AdditionalProperties["msteams"])["entities"] = chat.Mentions;

                    if (chat.Body != null) { chat.Body.Content = GenerateMentionFromEntities(entities.Result.Select(item => (entities.Result.IndexOf(item), item.name)).ToList()) + chat.Body.Content; }
                }
                else { Log.Debug("AdaptiveCardBuilder | no tagged team members to add"); }
            }
            else { Log.Debug("AdaptiveCardBuilder | card null"); }
        }

        private static List<ChatMessageMention> GenerateAllEntitiesMentionList(List<(string id, string name)> mentions)
        {
            var entities = new List<ChatMessageMention>();

            mentions.ForEach(item =>
            {
                (string id, string name) = item;
                entities.Add(GenerateEntityMention(mentions.IndexOf(item), id, name));
            });

            return entities;
        }

        private static ChatMessageMention GenerateEntityMention(int index, string id, string name)
        {
            var mention = new ChatMessageMention
            {
                Id = index,
                MentionText = $"{name}",
                Mentioned = new ChatMessageMentionedIdentitySet() 
                {
                    User = new Identity() 
                    { 
                        Id = id,
                        DisplayName = name
                    }
                }
            };

            return mention;
        }
        private static async Task<List<(string id, string name)>> GetAllTaggedTeamMemberDetails(List<string> usernames, string token)
        {
            List<(string id, string name)> details = new List<(string id, string name)>();

            foreach (var user in usernames)
            {

                string check = CheckUsername(user);

                var result = await GetTaggedTeamMemberByEmail(check, token);
                if (result.id != "" && result.name != "")
                {
                    Log.Information($"AdaptiveCardBuilder | Adding {result.name} -> {result.id} to the tagged member list");
                    details.Add(result);
                }
            }

            return details;
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

            return (user["id"] == null ? "" : user["id"].ToString(), user["displayName"] == null ? "" : user["displayName"].ToString());
        }

        private static string GenerateMentionFromEntities(List<(int, string)> entities)
        {
            //extract the name of each team member to be mentioned
            var mentionString = String.Join(", ", entities.Select(e => $"<at id=\"{e.Item1}\">{e.Item2}</at>").ToArray());

            Log.Debug($"AdaptiveCardBuilder | Generated Mentions: {mentionString}");

            return mentionString;
        }
    }
}
