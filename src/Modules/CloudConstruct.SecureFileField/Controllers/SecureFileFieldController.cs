using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CloudConstruct.SecureFileField.Providers;
using CloudConstruct.SecureFileField.Settings;
using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.ContentPermissions.Models;
using Orchard.Core.Common.Models;
using Orchard.Roles.Models;
using Orchard.Security;
using Orchard.Core.Contents;

namespace CloudConstruct.SecureFileField.Controllers {

	public class SecureFileFieldController : Controller {

		private readonly IOrchardServices _services;
        private readonly IAuthorizationService _authorizationService;
        private readonly IEncryptionService _encryptionService;
        private static readonly string[] AnonymousRole = new[] { "Anonymous" };
        private static readonly string[] AuthenticatedRole = new[] { "Authenticated" };

        public SecureFileFieldController(IOrchardServices services, IAuthorizationService authorizationService, IEncryptionService encryptionService)
        {
			_services = services;
            _authorizationService = authorizationService;
            _encryptionService = encryptionService;
        }

        /// <summary>
        /// Endpoint to retrieve a secured file. Content Item permissions are inherited from the parent content item.
        /// </summary>
        /// <param name="id">Unique Id on Parent Content Item</param>
        /// <param name="fieldName">Unique Field Name for the file field.</param>
        /// <returns></returns>
        [OutputCache(NoStore = true, Duration = 0)]
	    public FileResult GetSecureFile(int id, string fieldName) {
	        var accessGranted = false;
	        WorkContext wc = _services.WorkContext;
	        IUser user = _services.WorkContext.CurrentUser;

	        if (!String.IsNullOrEmpty(wc.CurrentSite.SuperUser)
	            && user != null
	            && String.Equals(user.UserName, wc.CurrentSite.SuperUser, StringComparison.Ordinal)) {
	            accessGranted = true;
	        }

	        var content = _services.ContentManager.Get<ContentPart>(id);

	        if (content == null) {
	            return null;
	        }

	        var part = content.ContentItem.As<ContentPermissionsPart>();

	        // if the content item has no right attached, check on the container
	        if (part == null || !part.Enabled) {
	            var commonPart = part.As<CommonPart>();
	            if (commonPart != null && commonPart.Container != null) {
	                part = commonPart.As<ContentPermissionsPart>();
	            }
	        }

            //if we do not have access level permissions for this content item then we need to check if user can view the content
	        if (part == null || !part.Enabled) {
	            accessGranted = _authorizationService.TryCheckAccess(Permissions.ViewContent, user, content);
	        }
	        else {
                var hasOwnership = HasOwnership(user, content.ContentItem);

	            IEnumerable<string> authorizedRoles;

	            //we only care about view permission in this field
	            authorizedRoles = (hasOwnership ? part.ViewOwnContent : part.ViewContent).Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);

	            // determine what set of roles should be examined by the access check
	            IEnumerable<string> rolesToExamine;
	            if (user == null) {
	                rolesToExamine = AnonymousRole;
	            }
	            else if (user.Has<IUserRoles>()) {
	                // the current user is not null, so get his roles and add "Authenticated" to it
	                rolesToExamine = user.As<IUserRoles>().Roles;

	                // when it is a simulated anonymous user in the admin
	                if (!rolesToExamine.Contains(AnonymousRole[0])) {
	                    rolesToExamine = rolesToExamine.Concat(AuthenticatedRole);
	                }
	            }
	            else {
	                // the user is not null and has no specific role, then it's just "Authenticated"
	                rolesToExamine = AuthenticatedRole;
	            }

	            accessGranted = rolesToExamine.Any(x => authorizedRoles.Contains(x, StringComparer.OrdinalIgnoreCase));

	        }

	        if (accessGranted)
            {

                var field = (Fields.SecureFileField)(content.ContentItem).Parts.SelectMany(p => p.Fields).First(f => f.Name == fieldName);
                var settings = field.PartFieldDefinition.Settings.GetModel<SecureFileFieldSettings>();
	            IStorageProvider provider;

                if (!string.IsNullOrEmpty(settings.SecureBlobAccountName)) {
                
                    provider = new SecureAzureBlobStorageProvider(settings.SecureBlobAccountName, settings.SecureSharedKey,
                                                                                                 settings.SecureBlobEndpoint, true, settings.SecureDirectoryName);
	            }
	            else {
                    // I need to check if, by setting, the file is in a subfolder.
                    string repo = settings.SecureDirectoryName;
                    if (!string.IsNullOrWhiteSpace(field.Subfolder))
                        repo = Path.Combine(repo, field.Subfolder);
	                provider = new SecureFileStorageProvider(repo);
	            }

                IStorageFile file = provider.Get<StorageFile>(field.Url);
                Stream fs = new MemoryStream(file.FileBytes);

                if (settings.EncryptFile) {
                    byte[] fileBytes = new byte[fs.Length];
                    fs.Read(fileBytes, 0, (int)fs.Length);
                    fileBytes = _encryptionService.Decode(fileBytes);
                    fs = new MemoryStream(fileBytes);
                }

                string mimeType = MimeMapping.GetMimeMapping(file.FileName);

                return new FileStreamResult(fs, mimeType);
            }

	        return null;
	    }

	    private static bool HasOwnership(IUser user, IContent content)
        {
            if (user == null || content == null)
                return false;

            var common = content.As<ICommonPart>();
            if (common == null || common.Owner == null)
                return false;

            return user.Id == common.Owner.Id;
        }
	}
}