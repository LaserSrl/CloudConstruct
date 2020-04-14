using Orchard.ContentManagement.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CloudConstruct.SecureFileField.Handlers {
    public class SecureFileFieldHandler : ContentHandler {
        protected override void Loading(LoadContentContext context) {
            base.Loading(context);

            var fields = context.ContentItem.Parts.SelectMany(x => x.Fields.Where(f => f.FieldDefinition.Name == typeof(SecureFileField.Fields.SecureFileField).Name)).Cast<SecureFileField.Fields.SecureFileField>();

            foreach (var field in fields) {
                var localField = field;
                field._secureUrl.Loader(() => "/CloudConstruct.SecureFileField/SecureFileField/GetSecureFile/" + context.ContentItem.Id + "?fieldName=" + field.Name);
            }
        }
    }
}