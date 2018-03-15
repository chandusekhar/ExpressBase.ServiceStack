﻿using ExpressBase.Common;
using ExpressBase.Common.Constants;
using ExpressBase.Common.Data;
using ExpressBase.Common.EbServiceStack.ReqNRes;
using ExpressBase.Common.ServiceClients;
using ExpressBase.Objects.ServiceStack_Artifacts;
using ExpressBase.ServiceStack.Auth0;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Discovery.Redis;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.ProtoBuf;
using ServiceStack.RabbitMq;
using ServiceStack.Redis;
using System;
using System.IdentityModel.Tokens.Jwt;
using static ExpressBase.ServiceStack.Services.ServerEventsSSServices;

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
            services.AddDataProtection(opts =>
             {
                 opts.ApplicationDiscriminator = "expressbase.servicestack";
             });
            // Add framework services.
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
        }
    }

    public class AppHost : AppHostBase
    {
        //public EbLiveSettings EbLiveSettings { get; set; }

        private PooledRedisClientManager RedisBusPool { get; set; }

        public AppHost() : base("EXPRESSbase Services", typeof(AppHost).Assembly) { }

        public override void OnAfterConfigChanged()
        {
            base.OnAfterConfigChanged();
        }

        public override void Configure(Container container)
        {
            var co = this.Config;
            LogManager.LogFactory = new ConsoleLogFactory(debugEnabled: true);

            var jwtprovider = new JwtAuthProvider
            {
                HashAlgorithm = "RS256",
                PrivateKeyXml = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_JWT_PRIVATE_KEY_XML),
                PublicKeyXml = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_JWT_PUBLIC_KEY_XML),
#if (DEBUG)
                RequireSecureConnection = false,
                //EncryptPayload = true,
#endif
                CreatePayloadFilter = (payload, session) =>
                {
                    payload["sub"] = (session as CustomUserSession).UserAuthId;
                    payload["cid"] = (session as CustomUserSession).CId;
                    payload["uid"] = (session as CustomUserSession).Uid.ToString();
                    payload["wc"] = (session as CustomUserSession).WhichConsole;
                },

                ExpireTokensIn = TimeSpan.FromMinutes(5),
                ExpireRefreshTokensIn = TimeSpan.FromHours(8),
                PersistSession = true,
                SessionExpiry = TimeSpan.FromHours(12)
            };
            //            var apikeyauthprovider = new ApiKeyAuthProvider(AppSettings)
            //            {
            //#if (DEBUG)
            //                RequireSecureConnection = false,
            //                //EncryptPayload = true,
            //#endif
            //                PersistSession = true,
            //                SessionExpiry = TimeSpan.FromHours(12)
            //            };

            this.Plugins.Add(new CorsFeature(allowedHeaders: "Content-Type, Authorization, Access-Control-Allow-Origin, Access-Control-Allow-Credentials"));
            this.Plugins.Add(new ProtoBufFormat());
            //this.Plugins.Add(new ServerEventsFeature());

            this.Plugins.Add(new AuthFeature(() => new CustomUserSession(),
                new IAuthProvider[] {
                    new MyFacebookAuthProvider(AppSettings)
                    {
                        AppId = "151550788692231",
                        AppSecret = "94ec1a04342e5cf7e7a971f2eb7ad7bc",
                        Permissions = new string[] { "email, public_profile" }
                    },

                    new MyTwitterAuthProvider(AppSettings)
                    {
                        ConsumerKey = "6G9gaYo7DMx1OHYRAcpmkPfvu",
                        ConsumerSecret = "Jx8uUIPeo5D0agjUnqkKHGQ4o6zTrwze9EcLtjDlOgLnuBaf9x",
                       // CallbackUrl = "http://localhost:8000/auth/twitter",
                        
                       // RequestTokenUrl= "https://api.twitter.com/oauth/authenticate",
                        
                    },

                    new MyGithubAuthProvider(AppSettings)
                    {
                    ClientId="4504eefeb8f027c810dd",
                    ClientSecret="d9c1c956a9fddd089798e0031851e93a8d0e5cc6",
                    RedirectUrl ="http://localhost:8000/"
                    },

                    new MyCredentialsAuthProvider(AppSettings)
                    {
                        PersistSession = true
                    },

                    jwtprovider,
                    //apikeyauthprovider

                }));

            //Also works but it's recommended to handle 404's by registering at end of .NET Core pipeline
            //this.CustomErrorHttpHandlers[HttpStatusCode.NotFound] = new RazorHandler("/notfound");

            this.ContentTypes.Register(MimeTypes.ProtoBuf, (reqCtx, res, stream) => ProtoBuf.Serializer.NonGeneric.Serialize(stream, res), ProtoBuf.Serializer.NonGeneric.Deserialize);

            

            SetConfig(new HostConfig { DebugMode = true });
            SetConfig(new HostConfig { DefaultContentType = MimeTypes.Json });

            var redisConnectionString = string.Format("redis://{0}@{1}:{2}",
               Environment.GetEnvironmentVariable(EnvironmentConstants.EB_REDIS_PASSWORD),
               Environment.GetEnvironmentVariable(EnvironmentConstants.EB_REDIS_SERVER),
               Environment.GetEnvironmentVariable(EnvironmentConstants.EB_REDIS_PORT));

            container.Register<IRedisClientsManager>(c => new RedisManagerPool(redisConnectionString));

            container.Register<IUserAuthRepository>(c => new EbRedisAuthRepository(c.Resolve<IRedisClientsManager>()));

            container.Register<JwtAuthProvider>(jwtprovider);
            container.RegisterAutoWiredAs<MemoryChatHistory, IChatHistory>();

            //SetConfig(new HostConfig
            //{
            //    WebHostUrl = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_SERVICESTACK_INT_URL)
            //});

            //Plugins.Add(new RedisServiceDiscoveryFeature());

            container.Register<IEbConnectionFactory>(c => new EbConnectionFactory(c)).ReusedWithin(ReuseScope.Request);

            container.Register<IEbServerEventClient>(c => new EbServerEventClient(c)).ReusedWithin(ReuseScope.Request);
            container.Register<IEbMqClient>(c => new EbMqClient(c)).ReusedWithin(ReuseScope.Request);
            container.Register<IEbStaticFileClient>(c => new EbStaticFileClient(c)).ReusedWithin(ReuseScope.Request);

            RabbitMqMessageFactory rabitFactory = new RabbitMqMessageFactory();
            rabitFactory.ConnectionFactory.UserName = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_USER);
            rabitFactory.ConnectionFactory.Password = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_PASSWORD);
            rabitFactory.ConnectionFactory.HostName = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_HOST);
            rabitFactory.ConnectionFactory.Port = Convert.ToInt32(Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_PORT));
            rabitFactory.ConnectionFactory.VirtualHost = Environment.GetEnvironmentVariable(EnvironmentConstants.EB_RABBIT_VHOST);

            var mqServer = new RabbitMqServer(rabitFactory);
            mqServer.RetryCount = 1;
            //mqServer.RegisterHandler<EmailServicesMqRequest>(base.ExecuteMessage);
            //mqServer.RegisterHandler<SMSSentMqRequest>(base.ExecuteMessage);
            //mqServer.RegisterHandler<RefreshSolutionConnectionsMqRequest>(base.ExecuteMessage);
            //mqServer.RegisterHandler<SMSStatusLogMqRequest>(base.ExecuteMessage);
            //mqServer.RegisterHandler<UploadFileAsyncRequest>(base.ExecuteMessage);
            //mqServer.RegisterHandler<ImageResizeMqRequest>(base.ExecuteMessage);
            //mqServer.RegisterHandler<FileMetaPersistMqRequest>(base.ExecuteMessage);
            //mqServer.RegisterHandler<SlackPostMqRequest>(base.ExecuteMessage);
            //mqServer.RegisterHandler<SlackAuthMqRequest>(base.ExecuteMessage);

            mqServer.Start();

            container.AddScoped<IMessageProducer, RabbitMqProducer>(serviceProvider =>
            {
                return mqServer.CreateMessageProducer() as RabbitMqProducer;
            });

            container.AddScoped<IMessageQueueClient, RabbitMqQueueClient>(serviceProvider =>
            {
                return mqServer.CreateMessageQueueClient() as RabbitMqQueueClient;
            });

            //Add a request filter to check if the user has a session initialized
            this.GlobalRequestFilters.Add((req, res, requestDto) =>
            {
                ILog log = LogManager.GetLogger(GetType());

                log.Info("In GlobalRequestFilters");
                try
                {
                    if (requestDto.GetType() == typeof(Authenticate))
                    {
                        log.Info("In Authenticate");

                        string TenantId = (requestDto as Authenticate).Meta != null ? (requestDto as Authenticate).Meta["cid"] : CoreConstants.EXPRESSBASE;
                        log.Info(TenantId);
                        RequestContext.Instance.Items.Add(CoreConstants.SOLUTION_ID, TenantId);
                    }
                }
                catch (Exception e)
                {
                    log.Info("ErrorStackTrace..........." + e.StackTrace);
                    log.Info("ErrorMessage..........." + e.Message);
                    log.Info("InnerException..........." + e.InnerException);
                }
                try
                {
                    if (requestDto != null && requestDto.GetType() != typeof(Authenticate) && requestDto.GetType() != typeof(GetAccessToken) && requestDto.GetType() != typeof(UniqueRequest) && requestDto.GetType() != typeof(CreateAccountRequest)&& requestDto.GetType() != typeof(EmailServicesMqRequest) && requestDto.GetType() != typeof(RegisterRequest) && requestDto.GetType() != typeof(AutoGenEbIdRequest)
                    && requestDto.GetType() != typeof(GetEventSubscribers) && requestDto.GetType() != typeof(GetChatHistory) && requestDto.GetType() != typeof(PostChatToChannel))
                    {
                        var auth = req.Headers[HttpHeaders.Authorization];
                        if (string.IsNullOrEmpty(auth))
                            res.ReturnAuthRequired();
                        else
                        {
                            var jwtoken = new JwtSecurityToken(auth.Replace("Bearer", string.Empty).Trim());
                            foreach (var c in jwtoken.Claims)
                            {
                                if (c.Type == "cid" && !string.IsNullOrEmpty(c.Value))
                                {
                                    RequestContext.Instance.Items.Add(CoreConstants.SOLUTION_ID, c.Value);
                                    if (requestDto is IEbSSRequest)
                                        (requestDto as IEbSSRequest).TenantAccountId = c.Value;
                                    if (requestDto is EbServiceStackRequest)
                                        (requestDto as EbServiceStackRequest).TenantAccountId = c.Value;
                                    continue;
                                }
                                if (c.Type == "uid" && !string.IsNullOrEmpty(c.Value))
                                {
                                    RequestContext.Instance.Items.Add("UserId", Convert.ToInt32(c.Value));
                                    if (requestDto is IEbSSRequest)
                                        (requestDto as IEbSSRequest).UserId = Convert.ToInt32(c.Value);
                                    if (requestDto is EbServiceStackRequest)
                                        (requestDto as EbServiceStackRequest).UserId = Convert.ToInt32(c.Value);
                                    continue;
                                }
                                if (c.Type == "wc" && !string.IsNullOrEmpty(c.Value))
                                {
                                    RequestContext.Instance.Items.Add("wc", c.Value);
                                    if (requestDto is EbServiceStackRequest)
                                        (requestDto as EbServiceStackRequest).WhichConsole = c.Value.ToString();
                                    continue;
                                }
                                if (c.Type == "sub" && !string.IsNullOrEmpty(c.Value))
                                {
                                    RequestContext.Instance.Items.Add("sub", c.Value);
                                    if (requestDto is EbServiceStackRequest)
                                        (requestDto as EbServiceStackRequest).UserAuthId = c.Value.ToString();
                                    continue;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    log.Info("ErrorStackTraceNontokenServices..........." + e.StackTrace);
                    log.Info("ErrorMessageNontokenServices..........." + e.Message);
                    log.Info("InnerExceptionNontokenServices..........." + e.InnerException);
                }
            });

            this.GlobalResponseFilters.Add((req, res, responseDto) =>
            {
                if (responseDto.GetResponseDto() != null)
                {
                    if (responseDto.GetResponseDto().GetType() == typeof(GetAccessTokenResponse))
                    {
                        //res.SetSessionCookie("Token", (res.Dto as GetAccessTokenResponse).AccessToken);
                    }
                }
            });

            this.GlobalRequestFilters.Add((req, res, requestDto) =>
            {
                if (req.RawUrl.Contains("smscallback"))
                {
                    req.Headers.Add("BearerToken", "");
                    
                }
            });
            //AfterInitCallbacks.Add(host =>
            //{

            //    var authProvider = (ApiKeyAuthProvider)AuthenticateService.GetAuthProvider(ApiKeyAuthProvider.Name);
            //    var authRepo = (IManageApiKeys)host.TryResolve<IAuthRepository>();
            //    var userRepo = (IUserAuthRepository)host.TryResolve<IUserAuthRepository>();

            //    try
            //    {
            //        IEnumerable<ApiKey> keys = authProvider.GenerateNewApiKeys("62");
            //        authRepo.StoreAll(keys);

            //    }
            //    catch (Exception e)
            //    {
            //        throw;
            //    }

            //});
        }
    }

}
