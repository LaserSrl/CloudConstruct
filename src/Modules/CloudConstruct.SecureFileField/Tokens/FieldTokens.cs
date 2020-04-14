using Orchard;
using Orchard.Fields.Tokens;
using Orchard.Localization;
using Orchard.Services;
using System;
using System.Globalization;
using CloudConstruct.SecureFileField.Fields;

namespace CloudConstruct.SecureFileField.Tokens {

    public class FieldTokens : ITokenProvider {

        private readonly IClock _clock;
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly Lazy<CultureInfo> _cultureInfo;
        private readonly Lazy<TimeZoneInfo> _timeZone;


        public FieldTokens(
            IClock clock,
            IWorkContextAccessor workContextAccessor) {
            _clock = clock;
            _workContextAccessor = workContextAccessor;

            _cultureInfo = new Lazy<CultureInfo>(() => CultureInfo.GetCultureInfo(_workContextAccessor.GetContext().CurrentCulture));
            _timeZone = new Lazy<TimeZoneInfo>(() => _workContextAccessor.GetContext().CurrentTimeZone);

            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public void Describe(dynamic context) {


            context.For("SecureFileField", T("Secure Fuke Field"), T("Tokens for Secure File Fields"))
                .Token("Url", T("Url"), T("The url of the media."))
                .Token("AlternateText", T("Alternate Text"), T("The alternate text of the media."))
                .Token("Class", T("Class"), T("The class of the media."))
                .Token("Style", T("Style"), T("The style of the media."))
                .Token("Alignment", T("Alignment"), T("The alignment of the media."))
                .Token("Width", T("Width"), T("The width of the media."))
                .Token("Height", T("Height"), T("The height of the media."))
                ;
        }

        public void Evaluate(dynamic context) {

            context.For<CloudConstruct.SecureFileField.Fields.SecureFileField>("SecureFileField")
                .Token("Url", (Func<CloudConstruct.SecureFileField.Fields.SecureFileField, object>)(field => field.Url))
                .Chain("Url", "Url", (Func<CloudConstruct.SecureFileField.Fields.SecureFileField, object>)(field => field.Url))
                .Token("AlternateText", (Func<CloudConstruct.SecureFileField.Fields.SecureFileField, object>)(field => field.AlternateText))
                .Token("Class", (Func<CloudConstruct.SecureFileField.Fields.SecureFileField, object>)(field => field.Class))
                .Token("Style", (Func<CloudConstruct.SecureFileField.Fields.SecureFileField, object>)(field => field.Style))
                .Token("Alignment", (Func<CloudConstruct.SecureFileField.Fields.SecureFileField, object>)(field => field.Alignment))
                .Token("Width", (Func<CloudConstruct.SecureFileField.Fields.SecureFileField, object>)(field => field.Width))
                .Token("Height", (Func<CloudConstruct.SecureFileField.Fields.SecureFileField, object>)(field => field.Height))
                ;
        }
    }
}