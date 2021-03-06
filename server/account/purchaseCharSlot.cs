﻿using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using common;
using MySql.Data.MySqlClient;

namespace server.account
{
    internal class purchaseCharSlot : RequestHandler
    {
        public override void HandleRequest(HttpListenerContext context)
        {
            NameValueCollection query;
            using (var rdr = new StreamReader(context.Request.InputStream))
                query = HttpUtility.ParseQueryString(rdr.ReadToEnd());

            using (var db = new Database())
            {
                Account acc = db.Verify(query["guid"], query["password"]);
                byte[] status;
                if (acc == null)
                {
                    status = Encoding.UTF8.GetBytes("<Error>Bad login</Error>");
                }
                else
                {
                    MySqlCommand cmd = db.CreateQuery();
                    cmd.CommandText = "SELECT fame FROM stats WHERE accId=@accId;";
                    cmd.Parameters.AddWithValue("@accId", acc.AccountId);
                    if ((int) cmd.ExecuteScalar() < 1000)
                        status = Encoding.UTF8.GetBytes("<Error>Not enough fame</Error>");
                    else
                    {
                        cmd = db.CreateQuery();
                        cmd.CommandText = "UPDATE stats SET fame = fame - 1000 WHERE accId=@accId";
                        cmd.Parameters.AddWithValue("@accId", acc.AccountId);
                        if (cmd.ExecuteNonQuery() > 0)
                        {
                            cmd = db.CreateQuery();
                            cmd.CommandText = "UPDATE accounts SET maxCharSlot = maxCharSlot + 1 WHERE id=@accId";
                            cmd.Parameters.AddWithValue("@accId", acc.AccountId);
                            if (cmd.ExecuteNonQuery() > 0)
                                status = Encoding.UTF8.GetBytes("<Success/>");
                            else
                                status = Encoding.UTF8.GetBytes("<Error>Internal Error</Error>");
                        }
                        else
                            status = Encoding.UTF8.GetBytes("<Error>Internal Error</Error>");
                    }
                }
                context.Response.OutputStream.Write(status, 0, status.Length);
                db.Dispose();
            }
        }
    }
}