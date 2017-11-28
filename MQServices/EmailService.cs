﻿using MailKit.Net.Smtp;
using MimeKit;
using ServiceStack;
using ExpressBase.Objects.ServiceStack_Artifacts;
using System;
using System.Threading.Tasks;
using ServiceStack.Messaging;
using ExpressBase.Objects.EmailRelated;
using ExpressBase.Common;
using ExpressBase.Objects;
using ExpressBase.Common.Data;

namespace ExpressBase.ServiceStack
{
    public class EmailService : EbBaseService
    {
        public EmailService(IMessageProducer _mqp, IMessageQueueClient _mqc) : base(_mqp, _mqc) { }

        public class EmailServiceInternal : EbBaseService
        {
            public EmailServiceInternal(IMessageProducer _mqp, IMessageQueueClient _mqc) : base(_mqp, _mqc) { }

            


            public string Post(EmailServicesMqRequest request)
            {
                var _InfraDb = base.ResolveService<ITenantDbFactory>() as TenantDbFactory;
                var myService = base.ResolveService<EbObjectService>();
                var res =(EbObjectParticularVersionResponse)myService.Get(new EbObjectParticularVersionRequest() { RefId = request.refid });
                EbEmailTemplate ebEmailTemplate = new EbEmailTemplate();
                foreach (var element in res.Data)
                {
                     ebEmailTemplate = EbSerializers.Json_Deserialize(element.Json);
                }


                var myDs = base.ResolveService<EbObjectService>();
                var myDsres = (EbObjectParticularVersionResponse)myDs.Get(new EbObjectParticularVersionRequest() { RefId = ebEmailTemplate.DataSourceRefId });
                // get sql from ebdatasource and render the sql 
                EbDataSource ebDataSource = new EbDataSource();
                foreach (var element in myDsres.Data)
                {
                    ebDataSource = EbSerializers.Json_Deserialize(element.Json);
                }
                var ds = _InfraDb.ObjectsDB.DoQueries(ebDataSource.Sql);

                foreach(var table in ds.Tables)
                {
                    foreach(var col in table.Columns)
                    {

                    }
                }

                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("EXPRESSbase", "info@expressbase.com"));
                emailMessage.To.Add(new MailboxAddress("", request.To));
                emailMessage.Subject = request.Subject;
                emailMessage.Body = new TextPart("plain") { Text = request.refid };
                try
                {
                    using (var client = new SmtpClient())
                    {
                        client.LocalDomain = "www.expressbase.com";
                        client.Connect("smtp.gmail.com", 465, true);
                        client.Authenticate(new System.Net.NetworkCredential() { UserName = "expressbasesystems@gmail.com", Password = "ebsystems" });
                        client.Send(emailMessage);
                        client.Disconnect(true);
                    }
                }
                catch (Exception e)
                {
                    return e.Message;
                }
                return null;
            }
        }
    }

   

}
