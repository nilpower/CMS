﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Datory;
using SiteServer.CMS.Core;
using SiteServer.CMS.DataCache;
using SiteServer.CMS.Model;
using SiteServer.Plugin;
using SiteServer.Utils;

namespace SiteServer.CMS.Provider
{
    public class UserLogDao : IDatabaseDao
    {
        private readonly Repository<UserLogInfo> _repository;
        public UserLogDao()
        {
            _repository = new Repository<UserLogInfo>(AppSettings.DatabaseType, AppSettings.ConnectionString);
        }

        public string TableName => _repository.TableName;
        public List<TableColumn> TableColumns => _repository.TableColumns;
        private static class Attr
        {
            public const string Id = nameof(UserLogInfo.Id);
            public const string UserName = nameof(UserLogInfo.UserName);
            public const string AddDate = nameof(UserLogInfo.AddDate);
            public const string Action = nameof(UserLogInfo.Action);
        }

        public UserLogInfo Insert(string userName, UserLogInfo logInfo)
        {
            logInfo.UserName = userName;
            logInfo.AddDate = DateTime.Now;

            logInfo.Id = _repository.Insert(logInfo);

            return logInfo;
        }

        public void DeleteIfThreshold()
        {
            if (!ConfigManager.Instance.IsTimeThreshold) return;

            var days = ConfigManager.Instance.TimeThreshold;
            if (days <= 0) return;

            _repository.Delete(Q.Where(Attr.AddDate, "<", DateTime.Now.AddDays(-days)));
        }

        public void Delete(List<int> idList)
        {
            if (idList == null || idList.Count <= 0) return;

            _repository.Delete(Q.WhereIn(Attr.Id, idList));
        }

        public void DeleteAll()
        {
            _repository.Delete();
        }

        public int GetCount()
        {
            return _repository.Count();
        }

        public string GetSelectCommend()
        {
            return "SELECT ID, UserName, IPAddress, AddDate, Action, Summary FROM siteserver_UserLog";
        }

        public string GetSelectCommend(string userName, string keyword, string dateFrom, string dateTo)
        {
            if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(dateFrom) && string.IsNullOrEmpty(dateTo))
            {
                return GetSelectCommend();
            }

            var whereString = new StringBuilder("WHERE ");

            var isWhere = false;

            if (!string.IsNullOrEmpty(userName))
            {
                isWhere = true;
                whereString.AppendFormat("(UserName = '{0}')", AttackUtils.FilterSql(userName));
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                if (isWhere)
                {
                    whereString.Append(" AND ");
                }
                isWhere = true;
                whereString.AppendFormat("(Action LIKE '%{0}%' OR Summary LIKE '%{0}%')", AttackUtils.FilterSql(keyword));
            }

            if (!string.IsNullOrEmpty(dateFrom))
            {
                if (isWhere)
                {
                    whereString.Append(" AND ");
                }
                isWhere = true;
                whereString.Append($"(AddDate >= {SqlUtils.GetComparableDate(TranslateUtils.ToDateTime(dateFrom))})");
            }
            if (!string.IsNullOrEmpty(dateTo))
            {
                if (isWhere)
                {
                    whereString.Append(" AND ");
                }
                whereString.Append($"(AddDate <= {SqlUtils.GetComparableDate(TranslateUtils.ToDateTime(dateTo))})");
            }

            return "SELECT ID, UserName, IPAddress, AddDate, Action, Summary FROM siteserver_UserLog " + whereString;
        }

        public List<ILogInfo> List(string userName, int totalNum, string action)
        {
            var query = Q.Where(Attr.UserName, userName);
            if (!string.IsNullOrEmpty(action))
            {
                query.Where(Attr.Action, action);
            }

            query.Limit(totalNum);
            query.OrderByDesc(Attr.Id);

            return _repository.GetAll(query).Select(x => (ILogInfo)x).ToList();
        }

        public IList<UserLogInfo> ApiGetLogs(string userName, int offset, int limit)
        {
            return _repository.GetAll(Q
                .Where(Attr.UserName, userName)
                .Offset(offset)
                .Limit(limit)
                .OrderByDesc(Attr.Id));
        }
    }
}


//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Linq;
//using System.Text;
//using Dapper;
//using Dapper.Contrib.Extensions;
//using SiteServer.CMS.Database.Caches;
//using SiteServer.CMS.Database.Core;
//using SiteServer.CMS.Database.Models;
//using SiteServer.Plugin;
//using SiteServer.Utils;

//namespace SiteServer.CMS.Database.Repositories
//{
//    public class UserLog
//    {
//        public override string TableName => "siteserver_UserLog";

//        public override List<TableColumn> TableColumns => new List<TableColumn>
//        {
//            new TableColumn
//            {
//                AttributeName = nameof(UserLogInfo.Id),
//                DataType = DataType.Integer,
//                IsIdentity = true,
//                IsPrimaryKey = true
//            },
//            new TableColumn
//            {
//                AttributeName = nameof(UserLogInfo.UserName),
//                DataType = DataType.VarChar
//            },
//            new TableColumn
//            {
//                AttributeName = nameof(UserLogInfo.IpAddress),
//                DataType = DataType.VarChar
//            },
//            new TableColumn
//            {
//                AttributeName = nameof(UserLogInfo.AddDate),
//                DataType = DataType.DateTime
//            },
//            new TableColumn
//            {
//                AttributeName = nameof(UserLogInfo.Action),
//                DataType = DataType.VarChar
//            },
//            new TableColumn
//            {
//                AttributeName = nameof(UserLogInfo.Summary),
//                DataType = DataType.VarChar
//            }
//        };

//        private const string ParamUserName = "@UserName";
//        private const string ParamIpAddress = "@IPAddress";
//        private const string ParamAddDate = "@AddDate";
//        private const string ParamAction = "@Action";
//        private const string ParamSummary = "@Summary";

//        public void InsertObject(UserLogInfo logInfo)
//        {
//            const string sqlString = "INSERT INTO siteserver_UserLog(UserName, IPAddress, AddDate, Action, Summary) VALUES (@UserName, @IPAddress, @AddDate, @Action, @Summary)";

//            IDataParameter[] parameters =
//            {
//                GetParameter(ParamUserName, logInfo.UserName),
//                GetParameter(ParamIpAddress, logInfo.IpAddress),
//                GetParameter(ParamAddDate,logInfo.AddDate),
//                GetParameter(ParamAction, logInfo.Action),
//                GetParameter(ParamSummary, logInfo.Summary)
//            };

//            DatabaseApi.ExecuteNonQuery(ConnectionString, sqlString, parameters);
//        }

//        public void DeleteIfThreshold()
//        {
//            if (!ConfigManager.Instance.IsTimeThreshold) return;

//            var days = ConfigManager.Instance.TimeThreshold;
//            if (days <= 0) return;

//            DatabaseApi.ExecuteNonQuery(ConnectionString, $@"DELETE FROM siteserver_UserLog WHERE AddDate < {SqlUtils.GetComparableDateTime(DateTime.Now.AddDays(-days))}");
//        }

//        public void DeleteById(List<int> idList)
//        {
//            if (idList == null || idList.Count <= 0) return;

//            var sqlString =
//                $"DELETE FROM siteserver_UserLog WHERE ID IN ({TranslateUtils.ToSqlInStringWithoutQuote(idList)})";

//            DatabaseApi.ExecuteNonQuery(ConnectionString, sqlString);
//        }

//        public void DeleteAll()
//        {
//            const string sqlString = "DELETE FROM siteserver_UserLog";

//            DatabaseApi.ExecuteNonQuery(ConnectionString, sqlString);
//        }

//        public int GetCount()
//        {
//            var count = 0;
//            const string sqlString = "SELECT Count(ID) FROM siteserver_UserLog";

//            using (var rdr = DatabaseApi.ExecuteReader(ConnectionString, sqlString))
//            {
//                if (rdr.Read())
//                {
//                    count = DatabaseApi.GetInt(rdr, 0);
//                }
//                rdr.Close();
//            }

//            return count;
//        }

//        public int GetCount(string where)
//        {
//            var count = 0;
//            var sqlString = "SELECT Count(ID) FROM siteserver_UserLog";
//            if (!string.IsNullOrEmpty(where))
//                sqlString += " WHERE " + where;

//            using (var rdr = DatabaseApi.ExecuteReader(ConnectionString, sqlString))
//            {
//                if (rdr.Read())
//                {
//                    count = DatabaseApi.GetInt(rdr, 0);
//                }
//                rdr.Close();
//            }

//            return count;
//        }

//        public string GetSelectCommend()
//        {
//            return "SELECT ID, UserName, IPAddress, AddDate, Action, Summary FROM siteserver_UserLog";
//        }

//        public string GetSelectCommend(string userName, string keyword, string dateFrom, string dateTo)
//        {
//            if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(dateFrom) && string.IsNullOrEmpty(dateTo))
//            {
//                return GetSelectCommend();
//            }

//            var whereString = new StringBuilder("WHERE ");

//            var isWhere = false;

//            if (!string.IsNullOrEmpty(userName))
//            {
//                isWhere = true;
//                whereString.AppendFormat("(UserName = '{0}')", AttackUtils.FilterSql(userName));
//            }

//            if (!string.IsNullOrEmpty(keyword))
//            {
//                if (isWhere)
//                {
//                    whereString.Append(" AND ");
//                }
//                isWhere = true;
//                whereString.AppendFormat("(Action LIKE '%{0}%' OR Summary LIKE '%{0}%')", AttackUtils.FilterSql(keyword));
//            }

//            if (!string.IsNullOrEmpty(dateFrom))
//            {
//                if (isWhere)
//                {
//                    whereString.Append(" AND ");
//                }
//                isWhere = true;
//                whereString.Append($"(AddDate >= {SqlUtils.GetComparableDate(TranslateUtils.ToDateTime(dateFrom))})");
//            }
//            if (!string.IsNullOrEmpty(dateTo))
//            {
//                if (isWhere)
//                {
//                    whereString.Append(" AND ");
//                }
//                whereString.Append($"(AddDate <= {SqlUtils.GetComparableDate(TranslateUtils.ToDateTime(dateTo))})");
//            }

//            return "SELECT ID, UserName, IPAddress, AddDate, Action, Summary FROM siteserver_UserLog " + whereString;
//        }

//        public List<ILogInfo> List(string userName, int totalNum, string action)
//        {
//            var list = new List<ILogInfo>();
//            var sqlString = "SELECT * FROM siteserver_UserLog WHERE UserName = @UserName";

//            if (!string.IsNullOrEmpty(action))
//            {
//                sqlString += " And Action = @Action";
//            }
//            sqlString += " ORDER BY ID DESC";

//            var parameters = new List<IDataParameter>
//            {
//                GetParameter(ParamUserName, userName)
//            };
//            if (!string.IsNullOrEmpty(action))
//            {
//                parameters.Add(GetParameter(ParamAction, action));
//            }

//            using (var rdr = DatabaseApi.ExecuteReader(ConnectionString, sqlString, parameters.ToArray()))
//            {
//                while (rdr.Read())
//                {
//                    var i = 0;
//                    var info = new UserLogInfo(DatabaseApi.GetInt(rdr, i++), DatabaseApi.GetString(rdr, i++), DatabaseApi.GetString(rdr, i++), DatabaseApi.GetDateTime(rdr, i++), DatabaseApi.GetString(rdr, i++), DatabaseApi.GetString(rdr, i));
//                    list.Add(info);
//                }
//            }

//            return list;
//        }

//        public List<UserLogInfo> ApiGetLogs(string userName, int offset, int limit)
//        {
//            var sqlString =
//                SqlDifferences.GetSqlString(TableName, null, $"WHERE {nameof(UserLogInfo.UserName)} = @{nameof(UserLogInfo.UserName)}", "ORDER BY Id DESC", offset, limit);

//            using (var connection = GetConnection())
//            {
//                return connection.Query<UserLogInfo>(sqlString, new {UserName = userName}).ToList();
//            }
//        }

//        public UserLogInfo ApiInsert(string userName, UserLogInfo logInfo)
//        {
//            logInfo.UserName = userName;
//            logInfo.IpAddress = PageUtils.GetIpAddress();
//            logInfo.AddDate = DateTime.Now;

//            using (var connection = GetConnection())
//            {
//                var identity = connection.InsertObject(logInfo);
//                if (identity > 0)
//                {
//                    logInfo.Id = Convert.ToInt32(identity);
//                }
//            }

//            return logInfo;
//        }
//    }
//}
