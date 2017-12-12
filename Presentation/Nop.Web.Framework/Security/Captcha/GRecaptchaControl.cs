﻿using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Web.Framework.Extensions;

namespace Nop.Web.Framework.Security.Captcha
{
    /// <summary>
    /// Google reCAPTCHA control
    /// </summary>
    public class GRecaptchaControl
    {
        private const string RECAPTCHA_API_URL_HTTP_VERSION1 = "http://www.google.com/recaptcha/api/challenge?k={0}";
        private const string RECAPTCHA_API_URL_HTTPS_VERSION1 = "https://www.google.com/recaptcha/api/challenge?k={0}";
        private const string RECAPTCHA_API_URL_VERSION2 = "https://www.google.com/recaptcha/api.js?onload=onloadCallback&render=explicit";

        /// <summary>
        /// Identifier
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// reCAPTCHA theme
        /// </summary>
        public string Theme { get; set; }
        /// <summary>
        /// reCAPTCHA public key
        /// </summary>
        public string PublicKey { get; set; }
        /// <summary>
        /// Language
        /// </summary>
        public string Language { get; set; }

        private readonly ReCaptchaVersion _version;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="version">Version</param>
        public GRecaptchaControl(ReCaptchaVersion version = ReCaptchaVersion.Version1)
        {
            _version = version;
        }

        /// <summary>
        /// Render control
        /// </summary>
        /// <returns></returns>
        public string RenderControl()
        {
            SetTheme();

            if (_version == ReCaptchaVersion.Version1)
            {
                var scriptCaptchaOptionsTag = new TagBuilder("script")
                {
                    TagRenderMode = TagRenderMode.Normal
                };
                scriptCaptchaOptionsTag.Attributes.Add("type", MimeTypes.TextJavascript);
                scriptCaptchaOptionsTag.InnerHtml.AppendHtml(
                    $"var RecaptchaOptions = {{ theme: '{Theme}', tabindex: 0 }}; ");
                
                var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                var scriptLoadApiTag = new TagBuilder("script")
                {
                    TagRenderMode = TagRenderMode.Normal
                };
                var scriptSrc = webHelper.IsCurrentConnectionSecured() ? 
                    string.Format(RECAPTCHA_API_URL_HTTPS_VERSION1, PublicKey) :
                    string.Format(RECAPTCHA_API_URL_HTTP_VERSION1, PublicKey);
                scriptLoadApiTag.Attributes.Add("src", scriptSrc);

                return scriptCaptchaOptionsTag.RenderHtmlContent() + scriptLoadApiTag.RenderHtmlContent();
            }
            else if (_version == ReCaptchaVersion.Version2)
            {
                var scriptCallbackTag = new TagBuilder("script")
                {
                    TagRenderMode = TagRenderMode.Normal
                };
                scriptCallbackTag.Attributes.Add("type", MimeTypes.TextJavascript);
                scriptCallbackTag.InnerHtml.AppendHtml(
                    $"var onloadCallback = function() {{grecaptcha.render('{Id}', {{'sitekey' : '{PublicKey}', 'theme' : '{Theme}' }});}};");

                var captchaTag = new TagBuilder("div")
                {
                    TagRenderMode = TagRenderMode.Normal
                };
                captchaTag.Attributes.Add("id", Id);

                var scriptLoadApiTag = new TagBuilder("script")
                {
                    TagRenderMode = TagRenderMode.Normal
                };
                scriptLoadApiTag.Attributes.Add("src", RECAPTCHA_API_URL_VERSION2 + (string.IsNullOrEmpty(Language) ? "" : $"&hl={Language}"
                                                       ));
                scriptLoadApiTag.Attributes.Add("async", null);
                scriptLoadApiTag.Attributes.Add("defer", null);

                return scriptCallbackTag.RenderHtmlContent() + captchaTag.RenderHtmlContent() + scriptLoadApiTag.RenderHtmlContent();
            }

            throw new NotSupportedException("Specified version is not supported");
        }

        private void SetTheme()
        {
            if (Theme == null)
                Theme = "";
            Theme = Theme.ToLower();

            var themes = new[] {"white", "blackglass", "red", "clean", "light", "dark"};

            if (_version == ReCaptchaVersion.Version1)
            {
                switch (Theme)
                {
                    case "light":
                        Theme = "white";
                        break;
                    case "dark":
                        Theme = "blackglass";
                        break;
                    default:
                        if (!themes.Contains(Theme))
                        {
                            Theme = "white";
                        }
                        break;
                }
            }
            else if (_version == ReCaptchaVersion.Version2)
            {
                switch (Theme)
                {
                    case "clean":
                    case "red":
                    case "white":
                        Theme = "light";
                        break;
                    case "blackglass":
                        Theme = "dark";
                        break;
                    default:
                        if (!themes.Contains(Theme))
                        {
                            Theme = "light";
                        }
                        break;
                }
            }
        }
    }
}