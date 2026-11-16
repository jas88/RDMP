// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
using Rdmp.Core.DataExport.Data;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Settings;

namespace Rdmp.Core.CommandExecution.AtomicCommands;

class ExecuteCommandSendExtractionResolutionTeamsNotification: BasicCommandExecution,IAtomicCommand
{

    private readonly ExtractionConfiguration _ec;
    private readonly bool _success;
    // A webhook that does not answer must not hold up the end of an extraction for long
    private static readonly HttpClient SharedClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    /// <summary>
    /// What the notification is sent with (replaced in tests)
    /// </summary>
    internal HttpMessageInvoker Client { get; init; } = SharedClient;

    /// <summary>
    /// Where the notification is sent, by default <see cref="UserSettings.ExtractionWebhookUrl"/>
    /// </summary>
    internal string Url { get; init; } = UserSettings.ExtractionWebhookUrl;

    private readonly string template = "{  \"type\":\"AdaptiveCard\",  \"attachments\":[      {        \"contentType\":\"application/vnd.microsoft.card.adaptive\",        \"contentUrl\":null,        \"content\":{  \"type\": \"AdaptiveCard\",    \"$schema\": \"https://adaptivecards.io/schemas/adaptive-card.json\",    \"version\": \"1.5\",    \"body\": [        {            \"type\": \"Table\",            \"columns\": [                {                    \"width\": 1                },                {                    \"width\": 7                }            ],            \"rows\": [                {                    \"type\": \"TableRow\",                    \"cells\": [                        {                            \"type\": \"TableCell\",                            \"items\": [                                {                                    \"type\": \"Image\",                                    \"url\": \"\",                                    \"size\": \"Small\",                                    \"width\": \"40px\"                                }                            ]                        },                        {                            \"type\": \"TableCell\",                            \"verticalContentAlignment\": \"Center\",                            \"items\": [                                {                                    \"type\": \"TextBlock\",                                    \"text\": \"\",                                    \"wrap\": true,                                    \"maxLines\": 3                                }                            ],                            \"targetWidth\": \"Wide\",                            \"bleed\": true                        },                        {                            \"type\": \"TableCell\",                            \"isVisible\": false                        }                    ]                }            ]        },        {            \"type\": \"TextBlock\",            \"text\": \"<at></at>\"        }    ],    \"msteams\": {                \"entities\": [                    {                    \"type\": \"mention\",                    \"text\": \"<at></at>\",                    \"mentioned\": {                        \"id\": \"\",                        \"name\": \"\"                    }                    }                ]            }        }       }  ]}";
    public ExecuteCommandSendExtractionResolutionTeamsNotification(IBasicActivateItems activator, ExtractionConfiguration ec, bool success) : base(activator) {
        _ec = ec;
        _success = success;
    }

    public override void Execute()
    {
        base.Execute();

        var json = GetContent();

        try
        {
            // Wait for the request to finish.  The caller is usually about to exit (the command line returns as soon
            // as the extraction is over), which would cut off a request left running in the background.
            PostAsync(json).GetAwaiter().GetResult();
        }
        catch (Exception e) when (IsSendFailure(e) && BasicActivator?.GlobalErrorCheckNotifier != null)
        {
            // The extraction has already finished, so failing to announce it is reported rather than thrown
            BasicActivator.GlobalErrorCheckNotifier.OnCheckPerformed(new CheckEventArgs(
                $"Could not send the Teams notification for extraction '{_ec.Name}'", CheckResult.Warning, e));
        }
    }

    /// <summary>
    /// True for the ways sending can fail: no connection or an error response, a timeout, or a webhook url that
    /// cannot be used
    /// </summary>
    private static bool IsSendFailure(Exception e) =>
        e is HttpRequestException or OperationCanceledException or InvalidOperationException or UriFormatException;

    /// <summary>
    /// Posts <paramref name="json"/> to the webhook and throws if it is not accepted.  The request (and so its
    /// content) stays alive until the response has arrived.
    /// </summary>
    private async Task PostAsync(string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Url);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await Client.SendAsync(request, CancellationToken.None).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private string GetContent()
    {
        var badIcon = "https://upload.wikimedia.org/wikipedia/commons/thumb/c/cc/Cross_red_circle.svg/1200px-Cross_red_circle.svg.png";
        var goodIcon = "https://upload.wikimedia.org/wikipedia/commons/thumb/3/3b/Eo_circle_green_checkmark.svg/2048px-Eo_circle_green_checkmark.svg.png";
        var icon = _success ? goodIcon : badIcon;
        var subText = _success ? "completed successfully" : "failed";
        var email = UserSettings.ExtractionWebhookUsername;
        var mention = $"<at>{email}</at>";
        var adatpiveCard = AdaptiveCard.FromJson(template);
        adatpiveCard.Attachments[0].Content.Body[0].Rows[0].Cells[0].Items[0].Url = icon;
        adatpiveCard.Attachments[0].Content.Body[0].Rows[0].Cells[1].Items[0].Text = $"Extraction {_ec.Name}: {subText}.";
        adatpiveCard.Attachments[0].Content.Body[1].Text =mention;
        adatpiveCard.Attachments[0].Content.Msteams.Entities[0].Text = mention;
        adatpiveCard.Attachments[0].Content.Msteams.Entities[0].Mentioned.Id = email;
        adatpiveCard.Attachments[0].Content.Msteams.Entities[0].Mentioned.Name =email;


        return Serialize.ToJson(adatpiveCard);
    }
}

partial class AdaptiveCard
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("attachments")]
    public Attachment[] Attachments { get; set; }
}

partial class Attachment
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("contentUrl")]
    public object ContentUrl { get; set; }

    [JsonPropertyName("content")]
    public Content Content { get; set; }
}

partial class Content
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("$schema")]
    public Uri Schema { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("body")]
    public Body[] Body { get; set; }

    [JsonPropertyName("msteams")]
    public Msteams Msteams { get; set; }
}

partial class Body
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("columns")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Column[] Columns { get; set; }

    [JsonPropertyName("rows")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Row[] Rows { get; set; }

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Text { get; set; }
}

partial class Column
{
    [JsonPropertyName("width")]
    public long Width { get; set; }
}

partial class Row
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("cells")]
    public Cell[] Cells { get; set; }
}

partial class Cell
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("items")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Item[] Items { get; set; }

    [JsonPropertyName("verticalContentAlignment")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string VerticalContentAlignment { get; set; }

    [JsonPropertyName("targetWidth")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string TargetWidth { get; set; }

    [JsonPropertyName("bleed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Bleed { get; set; }

    [JsonPropertyName("isVisible")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsVisible { get; set; }
}

partial class Item
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Url { get; set; }

    [JsonPropertyName("size")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Size { get; set; }

    [JsonPropertyName("width")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Width { get; set; }

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Text { get; set; }

    [JsonPropertyName("wrap")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Wrap { get; set; }

    [JsonPropertyName("maxLines")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? MaxLines { get; set; }
}

partial class Msteams
{
    [JsonPropertyName("entities")]
    public Entity[] Entities { get; set; }
}

partial class Entity
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("mentioned")]
    public Mentioned Mentioned { get; set; }
}

partial class Mentioned
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

partial class AdaptiveCard
{
    public static AdaptiveCard FromJson(string json) => JsonSerializer.Deserialize<AdaptiveCard>(json, Converter.Options);
}

static class Serialize
{
    public static string ToJson(this AdaptiveCard self) => JsonSerializer.Serialize(self, Converter.Options);
}

internal static class Converter
{
    public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = false
    };
}