﻿using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Logging;
using ServiceStack.Mvc;
using ServiceStack.ProtoBuf;
using ServiceStack.Redis;

namespace ExpressBase.ServiceStack
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();
            services.AddMemoryCache();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseServiceStack(new AppHost());

            app.Use(new RazorHandler("/notfound"));
        }
    }

    public class AppHost : AppHostBase
    {
        public AppHost() : base("Test Razor", typeof(AppHost).GetAssembly()) { }

        public override void Configure(Container container)
        {
            LogManager.LogFactory = new ConsoleLogFactory(debugEnabled: true);

            this.Plugins.Add(new CorsFeature());
            this.Plugins.Add(new ProtoBufFormat());
           
            Plugins.Add(new AuthFeature(() => new CustomUserSession(),
                new IAuthProvider[] {
                    new MyJwtAuthProvider(AppSettings) {
                        //HashAlgorithm = "RS256",
                        //PrivateKeyXml = AppSettings.GetString("PrivateKeyXml"),
                        //RequireSecureConnection = true,
                        //EncryptPayload = true,
                        AuthKey = AesUtils.CreateKey(),
                        CreatePayloadFilter = (payload,session) => {
                            payload["iss"] = "eb-sec";
                            payload["aud"] = "eb-web";
                            payload["iat"] = ((System.Int32)session.CreatedAt.ToUniversalTime().Subtract(new System.DateTime(1970, 1, 1)).TotalSeconds).ToString();
                            payload["exp"] = ((System.Int32)session.CreatedAt.AddHours(3).ToUniversalTime().Subtract(new System.DateTime(1970, 1, 1)).TotalSeconds).ToString();
                            payload["uid"] = session.UserAuthId;
                            payload["email"] = session.UserName;
                            payload["cid"] = (session as CustomUserSession).CId;
                            payload["uid"] = (session as CustomUserSession).Uid.ToString();
                            payload["Fname"] =(session as CustomUserSession).FirstName;
                   
                        }
                    },
                    //new ApiKeyAuthProvider(AppSettings),        //Sign-in with API Key
                    //new CredentialsAuthProvider(),              //Sign-in with UserName/Password credentials
                    //new BasicAuthProvider(),                    //Sign-in with HTTP Basic Auth
                    //new DigestAuthProvider(AppSettings),        //Sign-in with HTTP Digest Auth
                    //new TwitterAuthProvider(AppSettings),       //Sign-in with Twitter
                    //new FacebookAuthProvider(AppSettings),      //Sign-in with Facebook
                    //new YahooOpenIdOAuthProvider(AppSettings),  //Sign-in with Yahoo OpenId
                    //new OpenIdOAuthProvider(AppSettings),       //Sign-in with Custom OpenId
                    //new GoogleOAuth2Provider(AppSettings),      //Sign-in with Google OAuth2 Provider
                    //new LinkedInOAuth2Provider(AppSettings),    //Sign-in with LinkedIn OAuth2 Provider
                    //new GithubAuthProvider(AppSettings),        //Sign-in with GitHub OAuth Provider
                    //new YandexAuthProvider(AppSettings),        //Sign-in with Yandex OAuth Provider        
                    //new VkAuthProvider(AppSettings),            //Sign-in with VK.com OAuth Provider 
                }));

            //Also works but it's recommended to handle 404's by registering at end of .NET Core pipeline
            //this.CustomErrorHttpHandlers[HttpStatusCode.NotFound] = new RazorHandler("/notfound");

            //container.RegisterAutoWired<TokenAuthorizationManager>().ReusedWithin(ReuseScope.Request);

            this.ContentTypes.Register(MimeTypes.ProtoBuf, (reqCtx, res, stream) => ProtoBuf.Serializer.NonGeneric.Serialize(stream, res), ProtoBuf.Serializer.NonGeneric.Deserialize);

            SetConfig(new HostConfig { DebugMode = true, DefaultContentType = MimeTypes.Json });
            //SetConfig(new HostConfig { DefaultContentType = MimeTypes.Json });

            var redisConnectionString = string.Format("{0}@{1}:{2}",
                AppSettings.Get<string>("RedisPassword"),
                AppSettings.Get<string>("RedisServer"),
                AppSettings.Get<int>("RedisPort"));
            container.Register<IRedisClientsManager>(c => new RedisManagerPool(redisConnectionString));
        }
    }
}
