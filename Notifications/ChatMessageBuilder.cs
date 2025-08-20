using Microsoft.Graph.Models;
using Serilog;
using TeamsFileNotifier.Messaging;
using TeamsFileNotifier.Notifications;

namespace teams_file_notifier.Notifications
{
    internal static class ChatMessageBuilder
    {
        public static ChatMessage GenerateChatMessage(UpdateTeamsRequestMessage message)
        {
            ChatMessage chat = new ChatMessage
            {
                Subject = null,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = "<attachment id=\"1\"></attachment>"  // placeholder for the adaptive card
                },
                Attachments = new List<ChatMessageAttachment>
                {
                    new ChatMessageAttachment
                    {
                        Id = "1",
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = ""
                    }
                },
                Mentions = new List<ChatMessageMention>()
            };

            string card = AdaptiveCardBuilder.BuildAdaptiveCard(message.Title, message.Filename, "", message.Content, message.IconURL, message.CustomActions, chat);

            chat.Attachments[0].Content = card;
            
            return chat;            
        }
    }
}
