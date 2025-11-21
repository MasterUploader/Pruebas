using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using System.Web.Http;
// Aliases para evitar ambigüedades y usar los atributos de WebApi 2
using HostAuthenticationAttribute = System.Web.Http.HostAuthenticationAttribute;
using OverrideAuthenticationAttribute = System.Web.Http.OverrideAuthenticationAttribute;
