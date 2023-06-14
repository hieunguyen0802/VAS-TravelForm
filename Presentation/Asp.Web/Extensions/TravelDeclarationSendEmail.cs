using Microsoft.AspNetCore.Http;
using src.Core.Domains;
using System;
using System.Collections.Generic;
using System.Net;

namespace src.Web.Extensions
{
    public static class TravelDeclarationSendEmail
    {
        public static string ReplaceMessageTemplateTokens(TravelDeclaration model, string websiteUrl, string template)
        {
            var tokens = new Dictionary<string, string>()
            {
                {"[Verifier]",
                    WebUtility.HtmlEncode(!string.IsNullOrEmpty(model.nameOfLineManager)?model.nameOfLineManager.Split('@')[0]:"")
                },
                 {"[ECSD]",
                    WebUtility.HtmlEncode(!string.IsNullOrEmpty(model.ECSDEmail)?model.ECSDEmail.Split('@')[0]:"")
                },
                {"[Requester]", WebUtility.HtmlEncode(model.Requester.FirstName + " "+model.Requester.LastName)},
                {"[RequestID]", WebUtility.HtmlEncode(model.request_id)},
                {"[linkforreview]", $"<a class='btn-primary' href=\"{websiteUrl}/TravelDeclaration/Edit/{model.TravelDeclarationId}\">here</a>"},
            };

            // Replaces tokens in the template with the values.
            foreach (string token in tokens.Keys)
                template = template.Replace(token, tokens[token]);

            return template;
        }
        public static string ReplaceMessageTemplateTokensForCovid(IncidentReport model, string websiteUrl, string template)
        {
            var tokens = new Dictionary<string, string>()
            {
                {"[Verifier]",
                    WebUtility.HtmlEncode(!string.IsNullOrEmpty(model.nameOfLineManager)?model.nameOfLineManager.Split('@')[0]:"")
                },
                 {"[ECSD]",
                    WebUtility.HtmlEncode(!string.IsNullOrEmpty(model.ECSDEmail)?model.ECSDEmail.Split('@')[0]:"")
                },
                {"[Requester]", WebUtility.HtmlEncode(model.Requester.FirstName + " "+model.Requester.LastName)},
                {"[RequestID]", WebUtility.HtmlEncode(model.request_id)},
                {"[linkforreview]", $"<a class='btn-primary' href=\"{websiteUrl}/CovidIncident/Edit/{model.IncidentReportId}\">here</a>"},
            };

            // Replaces tokens in the template with the values.
            foreach (string token in tokens.Keys)
                template = template.Replace(token, tokens[token]);

            return template;
        }

        public static string ReplaceMessageTemplateTokensRedZone(TravelDeclaration model, string websiteUrl, string template)
        {
            var tokens = new Dictionary<string, string>()
            {
                {"[Requester]", WebUtility.HtmlEncode(model.Requester.FirstName + " " + model.Requester.LastName)},
                {"[RequestID]", WebUtility.HtmlEncode(model.request_id)},
                {"[linkforreview]", $"<a class='btn-primary' href=\"{websiteUrl}/CovidIncident/CreateByRedZone/?travelId={model.TravelDeclarationId}&redZoneId={model.redZoneId}\">here</a>"},
            };

            // Replaces tokens in the template with the values.
            foreach (string token in tokens.Keys)
                template = template.Replace(token, tokens[token]);

            return template;
        }

    }
}
