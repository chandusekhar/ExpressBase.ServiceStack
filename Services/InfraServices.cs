﻿using ExpressBase.Common;
using ExpressBase.Data;
using MailKit.Security;
using MimeKit;
using ServiceStack;
using ServiceStack.Text;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ExpressBase.ServiceStack.Services
{
    [DataContract]
    public class InfraRequest : IReturn<InfraResponse>
    {
        [DataMember(Order = 0)]
        public Dictionary<string, object> Colvalues { get; set; }

        [DataMember(Order = 1)]
        public string ltype { get; set; }
    }

    [DataContract]
    public class InfraResponse
    {
        [DataMember(Order = 1)]
        public int id { get; set; }
    }

    [DataContract]
    [Route("/unc", "POST")]
    public class UnRequest : IReturn<bool>
    {
        [DataMember(Order = 0)]
        public Dictionary<string, object> Colvalues { get; set; }

    }

    [DataContract]
    public class DbCheckRequest : IReturn<bool>
    {
        [DataMember(Order = 0)]
        public Dictionary<string, object> DBColvalues { get; set; }

        [DataMember(Order = 1)]
        public int CId { get; set; }
    }

    [DataContract]
    public class AccountRequest : IReturn<bool>
    {
        [DataMember(Order = 0)]
        public Dictionary<string, object> Colvalues { get; set; }

        [DataMember(Order = 1)]
        public string op { get; set; }

        [DataMember(Order = 2)]
        public int CId { get; set; }
    }

    [DataContract]
    public class GetAccount : IReturn<AccountResponse>
    {
        [DataMember(Order = 0)]
        public int Uid { get; set; }
    }

    [DataContract]
    public class AccountResponse
    {
        [DataMember(Order = 1)]
        public List<string> aclist { get; set; }
    }

    [DataContract]
    public class SendMail : IReturn<bool>
    {
        [DataMember(Order = 0)]
        public Dictionary<string, object> Emailvals { get; set; }  
    }


    [ClientCanSwapTemplates]
    public class InfraServices : EbBaseService
    {
        public InfraResponse Any(InfraRequest request)
        {
            string path = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).FullName;
            var infraconf = EbSerializers.ProtoBuf_DeSerialize<EbInfraDBConf>(EbFile.Bytea_FromFile(Path.Combine(path, "EbInfra.conn")));
            var df = new DatabaseFactory(infraconf);
            using (var con = df.InfraDB.GetNewConnection())
            {
                con.Open();
                if (request.ltype == "fb")
                {

                    //DateTime date = DateTime.ParseExact(request.Colvalues["birthday"].ToString(), "MM/dd/yyyy", CultureInfo.InvariantCulture);
                    var cmd = df.InfraDB.GetNewCommand(con, "INSERT INTO eb_tenants (cname,firstname,gender,socialid) SELECT  @cname, @firstname,@gender,@socialid WHERE NOT EXISTS (SELECT socialid FROM eb_tenants WHERE socialid = @socialid) RETURNING id; ");
                    cmd.Parameters.Add(df.InfraDB.GetNewParameter("cname", System.Data.DbType.String, request.Colvalues["email"]));
                    cmd.Parameters.Add(df.InfraDB.GetNewParameter("firstname", System.Data.DbType.String, request.Colvalues["name"]));
                    //cmd.Parameters.Add(df.InfraDB.GetNewParameter("birthday", System.Data.DbType.DateTime, date));
                    cmd.Parameters.Add(df.InfraDB.GetNewParameter("gender", System.Data.DbType.String, request.Colvalues["gender"]));
                    cmd.Parameters.Add(df.InfraDB.GetNewParameter("socialid", System.Data.DbType.String, request.Colvalues["id"]));

                    InfraResponse res = new InfraResponse
                    {
                        id = Convert.ToInt32(cmd.ExecuteScalar())
                    };
                    return res;
                }
                else if (request.ltype == "G+")
                {

                    var cmd = df.InfraDB.GetNewCommand(con, "INSERT INTO eb_tenants (cname,firstname,gender,socialid) SELECT  @cname, @firstname,@gender,@socialid WHERE NOT EXISTS (SELECT socialid FROM eb_tenants WHERE socialid = @socialid) RETURNING id; ");
                    cmd.Parameters.Add(df.InfraDB.GetNewParameter("cname", System.Data.DbType.String, request.Colvalues["email"]));
                    cmd.Parameters.Add(df.InfraDB.GetNewParameter("firstname", System.Data.DbType.String, request.Colvalues["name"]));
                    cmd.Parameters.Add(df.InfraDB.GetNewParameter("gender", System.Data.DbType.String, request.Colvalues["gender"]));
                    cmd.Parameters.Add(df.InfraDB.GetNewParameter("socialid", System.Data.DbType.String, request.Colvalues["id"]));
                    InfraResponse res = new InfraResponse
                    {
                        id = Convert.ToInt32(cmd.ExecuteScalar())
                    };
                    return res;

                }
                else if (request.ltype == "update")
                {
                    var cmd = df.InfraDB.GetNewCommand(con, "UPDATE eb_tenants SET company=@company,employees=@employees,country=@country,phone=@phone WHERE id=@id RETURNING id");


                    cmd.Parameters.Add(df.InfraDB.GetNewParameter("company", System.Data.DbType.String, request.Colvalues["company"]));
                    cmd.Parameters.Add(df.InfraDB.GetNewParameter("employees", System.Data.DbType.String, request.Colvalues["employees"]));
                    cmd.Parameters.Add(df.InfraDB.GetNewParameter("country", System.Data.DbType.String, request.Colvalues["country"]));
                    cmd.Parameters.Add(df.InfraDB.GetNewParameter("phone", System.Data.DbType.String, request.Colvalues["phone"]));
                    cmd.Parameters.Add(df.InfraDB.GetNewParameter("id", System.Data.DbType.Int64, request.Colvalues["id"]));
                    InfraResponse res = new InfraResponse
                    {
                        id = Convert.ToInt32(cmd.ExecuteScalar())
                    };
                    return res;
                }
                else
                {

                    var cmd = df.InfraDB.GetNewCommand(con, "INSERT INTO eb_tenants (cname,firstname,password) VALUES ( @cname, @firstname,@password) RETURNING id;");

                    cmd.Parameters.Add(df.InfraDB.GetNewParameter("cname", System.Data.DbType.String, request.Colvalues["email"]));
                    cmd.Parameters.Add(df.InfraDB.GetNewParameter("firstname", System.Data.DbType.String, request.Colvalues["fullname"]));
                    cmd.Parameters.Add(df.InfraDB.GetNewParameter("password", System.Data.DbType.String, request.Colvalues["password"]));
                    InfraResponse res = new InfraResponse
                    {
                        id = Convert.ToInt32(cmd.ExecuteScalar())
                    };
                    return res;

                }
            }
        }

        //public bool Any(UnRequest request)
        //{
        //    string path = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).FullName;
        //    var infraconf = EbSerializers.ProtoBuf_DeSerialize<EbInfraDBConf>(EbFile.Bytea_FromFile(Path.Combine(path, "EbInfra.conn")));
        //    var df = new DatabaseFactory(infraconf);
        //    using (var con = df.InfraDB.GetNewConnection())
        //    {
        //        con.Open();

        //        foreach (string key in request.Colvalues.Keys)
        //        {
        //            string cf = request.Colvalues[key].ToString();
        //            var cmd = df.InfraDB.GetNewCommand(con, string.Format("SELECT COUNT(*) FROM eb_tenants where {0} = @{0}", key));
        //            cmd.Parameters.Add(df.InfraDB.GetNewParameter(string.Format("{0}", key), System.Data.DbType.String, request.Colvalues[key]));
        //            if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
        //                return false;

        //            else
        //                return true;
        //        }
        //        return false;
        //    }
        //}

        //public bool Any(DbCheckRequest request)
        //{

        //    string path = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).FullName;
        //    var infraconf = EbSerializers.ProtoBuf_DeSerialize<EbInfraDBConf>(EbFile.Bytea_FromFile(Path.Combine(path, "EbInfra.conn")));
        //    var df = new DatabaseFactory(infraconf);
        //    using (var con = df.InfraDB.GetNewConnection())
        //    {
        //        con.Open();
        //        string sql = string.Format("SELECT cid,cname FROM eb_tenants WHERE id={0}", request.CId);
        //        var dt = df.InfraDB.DoQuery(sql);



        //        //CREATE CLIENTDB CONN
        //        EbClientConf e = new EbClientConf()
        //        {
        //            ClientID = dt.Rows[0][0].ToString(),
        //            ClientName = dt.Rows[0][1].ToString(),
        //            EbClientTier = EbClientTiers.Unlimited
        //        };

        //        e.DatabaseConfigurations.Add(EbDatabaseTypes.EbOBJECTS, new EbDatabaseConfiguration(EbDatabaseTypes.EbOBJECTS, DatabaseVendors.PGSQL, request.DBColvalues["dbname"].ToString(), request.DBColvalues["sip"].ToString(), Convert.ToInt32(request.DBColvalues["pnum"]), request.DBColvalues["duname"].ToString(), request.DBColvalues["pwd"].ToString(), Convert.ToInt32(request.DBColvalues["tout"])));
        //        e.DatabaseConfigurations.Add(EbDatabaseTypes.EbDATA, new EbDatabaseConfiguration(EbDatabaseTypes.EbDATA, DatabaseVendors.PGSQL, request.DBColvalues["dbname"].ToString(), request.DBColvalues["sip"].ToString(), Convert.ToInt32(request.DBColvalues["pnum"]), request.DBColvalues["duname"].ToString(), request.DBColvalues["pwd"].ToString(), Convert.ToInt32(request.DBColvalues["tout"])));
        //        e.DatabaseConfigurations.Add(EbDatabaseTypes.EbFILES, new EbDatabaseConfiguration(EbDatabaseTypes.EbFILES, DatabaseVendors.PGSQL, request.DBColvalues["dbname"].ToString(), request.DBColvalues["sip"].ToString(), Convert.ToInt32(request.DBColvalues["pnum"]), request.DBColvalues["duname"].ToString(), request.DBColvalues["pwd"].ToString(), Convert.ToInt32(request.DBColvalues["tout"])));
        //        e.DatabaseConfigurations.Add(EbDatabaseTypes.EbLOGS, new EbDatabaseConfiguration(EbDatabaseTypes.EbLOGS, DatabaseVendors.PGSQL, request.DBColvalues["dbname"].ToString(), request.DBColvalues["sip"].ToString(), Convert.ToInt32(request.DBColvalues["pnum"]), request.DBColvalues["duname"].ToString(), request.DBColvalues["pwd"].ToString(), Convert.ToInt32(request.DBColvalues["tout"])));
        //        e.DatabaseConfigurations.Add(EbDatabaseTypes.EbOBJECTS_RO, new EbDatabaseConfiguration(EbDatabaseTypes.EbOBJECTS_RO, DatabaseVendors.PGSQL, request.DBColvalues["dbname"].ToString(), request.DBColvalues["sip"].ToString(), Convert.ToInt32(request.DBColvalues["pnum"]), request.DBColvalues["duname"].ToString(), request.DBColvalues["pwd"].ToString(), Convert.ToInt32(request.DBColvalues["tout"])));
        //        e.DatabaseConfigurations.Add(EbDatabaseTypes.EbDATA_RO, new EbDatabaseConfiguration(EbDatabaseTypes.EbDATA_RO, DatabaseVendors.PGSQL, request.DBColvalues["dbname"].ToString(), request.DBColvalues["sip"].ToString(), Convert.ToInt32(request.DBColvalues["pnum"]), request.DBColvalues["duname"].ToString(), request.DBColvalues["pwd"].ToString(), Convert.ToInt32(request.DBColvalues["tout"])));
        //        e.DatabaseConfigurations.Add(EbDatabaseTypes.EbFILES_RO, new EbDatabaseConfiguration(EbDatabaseTypes.EbFILES_RO, DatabaseVendors.PGSQL, request.DBColvalues["dbname"].ToString(), request.DBColvalues["sip"].ToString(), Convert.ToInt32(request.DBColvalues["pnum"]), request.DBColvalues["duname"].ToString(), request.DBColvalues["pwd"].ToString(), Convert.ToInt32(request.DBColvalues["tout"])));
        //        e.DatabaseConfigurations.Add(EbDatabaseTypes.EbLOGS_RO, new EbDatabaseConfiguration(EbDatabaseTypes.EbLOGS_RO, DatabaseVendors.PGSQL, request.DBColvalues["dbname"].ToString(), request.DBColvalues["sip"].ToString(), Convert.ToInt32(request.DBColvalues["pnum"]), request.DBColvalues["duname"].ToString(), request.DBColvalues["pwd"].ToString(), Convert.ToInt32(request.DBColvalues["tout"])));

        //        byte[] bytea2 = EbSerializers.ProtoBuf_Serialize(e);
        //        var dbconf = EbSerializers.ProtoBuf_DeSerialize<EbClientConf>(bytea2);

        //        var dbf = new DatabaseFactory(dbconf);
        //        var _con = dbf.ObjectsDB.GetNewConnection();
        //        try
        //        {
        //            _con.Open();
        //        }
        //        catch (Exception ex) { return false; }
        //        var cmd = df.InfraDB.GetNewCommand(con, "UPDATE eb_tenants SET conf=@conf WHERE id=@id;");
        //        cmd.Parameters.Add(df.InfraDB.GetNewParameter("conf", System.Data.DbType.Binary, bytea2));
        //        cmd.Parameters.Add(df.InfraDB.GetNewParameter("id", System.Data.DbType.Int64, Convert.ToInt32(request.DBColvalues["id"])));
        //        cmd.ExecuteNonQuery();
        //        return true;


        //    }
        //}

        public bool Any(AccountRequest request)
        {
            string path = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).FullName;
            var infraconf = EbSerializers.ProtoBuf_DeSerialize<EbInfraDBConf>(EbFile.Bytea_FromFile(Path.Combine(path, "EbInfra.conn")));
            var df = new DatabaseFactory(infraconf);
            using (var con = df.InfraDB.GetNewConnection())
            {
                con.Open();
               
                if(request.op=="insert")
                {

               
                var cmd = df.InfraDB.GetNewCommand(con, "INSERT INTO eb_tenantaccount(accountname,cid,address,phone,email,website,tier,tenantname,tenantid)VALUES(@accountname,@cid,@address,@phone,@email,@website,@tier,@tenantname,@tenantid)");
                cmd.Parameters.Add(df.InfraDB.GetNewParameter("accountname", System.Data.DbType.String, request.Colvalues["accountname"]));
                cmd.Parameters.Add(df.InfraDB.GetNewParameter("cid", System.Data.DbType.String, request.Colvalues["cid"]));
                cmd.Parameters.Add(df.InfraDB.GetNewParameter("address", System.Data.DbType.String, request.Colvalues["address"]));
                cmd.Parameters.Add(df.InfraDB.GetNewParameter("phone", System.Data.DbType.String, request.Colvalues["phone"]));
                cmd.Parameters.Add(df.InfraDB.GetNewParameter("email", System.Data.DbType.String, request.Colvalues["email"]));
                cmd.Parameters.Add(df.InfraDB.GetNewParameter("website", System.Data.DbType.String, request.Colvalues["website"]));
                cmd.Parameters.Add(df.InfraDB.GetNewParameter("tier", System.Data.DbType.String, request.Colvalues["tier"]));
                cmd.Parameters.Add(df.InfraDB.GetNewParameter("tenantname", System.Data.DbType.String, request.Colvalues["tenantname"]));
                cmd.Parameters.Add(df.InfraDB.GetNewParameter("tenantid", System.Data.DbType.Int64, request.Colvalues["tenantid"]));
                return (Convert.ToInt32(cmd.ExecuteScalar()) >= 0);
                }
                else if(request.op=="Dbcheck")
                {
                    string sql = string.Format("SELECT cid,cname FROM eb_tenants WHERE id={0}", request.CId);
                    var dt = df.InfraDB.DoQuery(sql);



                    //CREATE CLIENTDB CONN
                    EbClientConf e = new EbClientConf()
                    {
                        ClientID = dt.Rows[0][0].ToString(),
                        ClientName = dt.Rows[0][1].ToString(),
                        EbClientTier = EbClientTiers.Unlimited
                    };

                    e.DatabaseConfigurations.Add(EbDatabaseTypes.EbOBJECTS, new EbDatabaseConfiguration(EbDatabaseTypes.EbOBJECTS, DatabaseVendors.PGSQL, request.Colvalues["dbname"].ToString(), request.Colvalues["sip"].ToString(), Convert.ToInt32(request.Colvalues["pnum"]), request.Colvalues["duname"].ToString(), request.Colvalues["pwd"].ToString(), Convert.ToInt32(request.Colvalues["tout"])));
                    e.DatabaseConfigurations.Add(EbDatabaseTypes.EbDATA, new EbDatabaseConfiguration(EbDatabaseTypes.EbDATA, DatabaseVendors.PGSQL, request.Colvalues["dbname"].ToString(), request.Colvalues["sip"].ToString(), Convert.ToInt32(request.Colvalues["pnum"]), request.Colvalues["duname"].ToString(), request.Colvalues["pwd"].ToString(), Convert.ToInt32(request.Colvalues["tout"])));
                    e.DatabaseConfigurations.Add(EbDatabaseTypes.EbFILES, new EbDatabaseConfiguration(EbDatabaseTypes.EbFILES, DatabaseVendors.PGSQL, request.Colvalues["dbname"].ToString(), request.Colvalues["sip"].ToString(), Convert.ToInt32(request.Colvalues["pnum"]), request.Colvalues["duname"].ToString(), request.Colvalues["pwd"].ToString(), Convert.ToInt32(request.Colvalues["tout"])));
                    e.DatabaseConfigurations.Add(EbDatabaseTypes.EbLOGS, new EbDatabaseConfiguration(EbDatabaseTypes.EbLOGS, DatabaseVendors.PGSQL, request.Colvalues["dbname"].ToString(), request.Colvalues["sip"].ToString(), Convert.ToInt32(request.Colvalues["pnum"]), request.Colvalues["duname"].ToString(), request.Colvalues["pwd"].ToString(), Convert.ToInt32(request.Colvalues["tout"])));
                    e.DatabaseConfigurations.Add(EbDatabaseTypes.EbOBJECTS_RO, new EbDatabaseConfiguration(EbDatabaseTypes.EbOBJECTS_RO, DatabaseVendors.PGSQL, request.Colvalues["dbname"].ToString(), request.Colvalues["sip"].ToString(), Convert.ToInt32(request.Colvalues["pnum"]), request.Colvalues["duname"].ToString(), request.Colvalues["pwd"].ToString(), Convert.ToInt32(request.Colvalues["tout"])));
                    e.DatabaseConfigurations.Add(EbDatabaseTypes.EbDATA_RO, new EbDatabaseConfiguration(EbDatabaseTypes.EbDATA_RO, DatabaseVendors.PGSQL, request.Colvalues["dbname"].ToString(), request.Colvalues["sip"].ToString(), Convert.ToInt32(request.Colvalues["pnum"]), request.Colvalues["duname"].ToString(), request.Colvalues["pwd"].ToString(), Convert.ToInt32(request.Colvalues["tout"])));
                    e.DatabaseConfigurations.Add(EbDatabaseTypes.EbFILES_RO, new EbDatabaseConfiguration(EbDatabaseTypes.EbFILES_RO, DatabaseVendors.PGSQL, request.Colvalues["dbname"].ToString(), request.Colvalues["sip"].ToString(), Convert.ToInt32(request.Colvalues["pnum"]), request.Colvalues["duname"].ToString(), request.Colvalues["pwd"].ToString(), Convert.ToInt32(request.Colvalues["tout"])));
                    e.DatabaseConfigurations.Add(EbDatabaseTypes.EbLOGS_RO, new EbDatabaseConfiguration(EbDatabaseTypes.EbLOGS_RO, DatabaseVendors.PGSQL, request.Colvalues["dbname"].ToString(), request.Colvalues["sip"].ToString(), Convert.ToInt32(request.Colvalues["pnum"]), request.Colvalues["duname"].ToString(), request.Colvalues["pwd"].ToString(), Convert.ToInt32(request.Colvalues["tout"])));

                    byte[] bytea2 = EbSerializers.ProtoBuf_Serialize(e);
                    var dbconf = EbSerializers.ProtoBuf_DeSerialize<EbClientConf>(bytea2);

                    var dbf = new DatabaseFactory(dbconf);
                    var _con = dbf.ObjectsDB.GetNewConnection();
                    try
                    {
                        _con.Open();
                    }
                    catch (Exception ex) { return false; }
                    var cmd = df.InfraDB.GetNewCommand(con, "UPDATE eb_tenants SET conf=@conf WHERE id=@id;");
                    cmd.Parameters.Add(df.InfraDB.GetNewParameter("conf", System.Data.DbType.Binary, bytea2));
                    cmd.Parameters.Add(df.InfraDB.GetNewParameter("id", System.Data.DbType.Int64, Convert.ToInt32(request.Colvalues["id"])));
                    cmd.ExecuteNonQuery();
                    return true;
                }
                else
                {
                    return false;
                }

            }
        }

        //public bool Any(SendMail request)
        //{
        //    string path = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).FullName;
        //    var infraconf = EbSerializers.ProtoBuf_DeSerialize<EbInfraDBConf>(EbFile.Bytea_FromFile(Path.Combine(path, "EbInfra.conn")));
        //    var df = new DatabaseFactory(infraconf);
        //    using (var con = df.InfraDB.GetNewConnection())
        //    {
        //        con.Open();
        //        foreach (string key in request.Emailvals.Keys)
        //        {

        //            var cmd = df.InfraDB.GetNewCommand(con, string.Format("SELECT COUNT(*) FROM eb_tenants where cname = @{0}", key));
        //            cmd.Parameters.Add(df.InfraDB.GetNewParameter(string.Format("{0}", key), System.Data.DbType.String, request.Emailvals[key]));
        //            int i = (Convert.ToInt32(cmd.ExecuteScalar()));
        //            if (i > 0)
        //            {
        //                StringBuilder strBody = new StringBuilder();
        //                //Passing emailid,username and generated unique code via querystring. For testing pass your localhost number and while making online pass your domain name instead of localhost path.
        //                strBody.Append("<a href=http://localhost:53125/Tenant/ResetPassword.aspx?emailId=" + request.Emailvals["email"]+">Click here to change your password</a>");
        //                // sbody.Append("&uCode=" + uniqueCode + "&uName=" + txtUserName.Text + ">Click here to change your password</a>");

        //                var message = new MimeMessage();
        //                message.From.Add(new MailboxAddress("ExpressBase Systems", "shasisoman785@gmail.com"));
        //                message.To.Add(new MailboxAddress("", request.Emailvals["email"].ToString()));
        //                message.Subject = "EB Account Created";
        //                message.Body = new TextPart("plain")
        //                {
        //                    Text = strBody.ToString()
        //                };

        //                using (var client = new MailKit.Net.Smtp.SmtpClient())
        //                {
        //                    //client.Connect("smtp.gmail.com", 587, false);
        //                    client.Connect("smtp.gmail.com", 587, false);
        //                    client.AuthenticationMechanisms.Remove("XOAUTH2");

        //                    // Note: since we don't have an OAuth2 token, disable 	// the XOAUTH2 authentication mechanism.     client.Authenticate("anuraj.p@example.com", "password");
        //                    client.Send(message);
        //                    client.Disconnect(true);
        //                }
        //            }

        //            //return false;

        //            else
        //                return true;
        //        }
        //        return false;

        //    }

        //    }

        public AccountResponse Any(GetAccount request)
        {
            string path = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).FullName;
            
            var infraconf = EbSerializers.ProtoBuf_DeSerialize<EbInfraDBConf>(EbFile.Bytea_FromFile(Path.Combine(path, "EbInfra.conn")));
            var df = new DatabaseFactory(infraconf);
            using (var con = df.InfraDB.GetNewConnection())
            {
                con.Open();
                string sql = string.Format("SELECT accountname FROM eb_tenantaccount WHERE tenantid={0}", request.Uid );
                var dt = df.InfraDB.DoQuery(sql);
                List<string> list = new List<string>();
                foreach(EbDataRow dr in dt.Rows)
                {
                    list.Add(dr[0].ToString());
                }
                AccountResponse resp = new AccountResponse()
                {
                    aclist = list
                };
                return resp;
            }
        }
        
    }
}
