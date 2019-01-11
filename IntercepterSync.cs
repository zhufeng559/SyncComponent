using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using EFIntercepterSync.Helper;
using EFIntercepterSync.Model;

namespace EFIntercepterSync
{
    /// <summary>
    ///     ef拦截同步组件
    /// </summary>
    public class IntercepterSync : DbCommandInterceptor
    {
        /// <summary>
        ///     重写ScalarExecuting
        /// </summary>
        /// <param name="command"></param>
        /// <param name="interceptionContext"></param>
        public override void ScalarExecuting(DbCommand command,
            DbCommandInterceptionContext<object> interceptionContext)
        {
            //如果配置config启用同步功能
            if (ActiveMqHelper.ConfigList.Any())
                if (command.CommandText.StartsWith("insert", StringComparison.InvariantCultureIgnoreCase)
                    || command.CommandText.StartsWith("update", StringComparison.InvariantCultureIgnoreCase)
                    || command.CommandText.StartsWith("delete", StringComparison.InvariantCultureIgnoreCase))
                {
                    ////阻止原数据库写入
                    //interceptionContext.SuppressExecution();
                    ////让ef返回成功,不抛异常
                    //interceptionContext.Result = 1;
                    //return;

                    //判断是否含有事务,理论上保存后所有操作ef会自动包含事务
                    if (command.Transaction == null)
                    {
                        //获取完整的sql
                        var sql = SqlHelper.GetFullCommand(command);
                        ActiveMqHelper.ConfigList.ForEach(item =>
                        {
                            var model = new MqData { Sql = new List<string> { sql } };
                            ActiveMqHelper.SendMessage(item, model);
                        });
                    }
                }

            base.ScalarExecuting(command, interceptionContext);
        }

        /// <summary>
        ///     重写NonQueryExecuting
        /// </summary>
        /// <param name="command"></param>
        /// <param name="interceptionContext"></param>
        public override void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            //如果配置config启用同步功能
            if (ActiveMqHelper.ConfigList.Any())
                if (command.CommandText.StartsWith("insert", StringComparison.InvariantCultureIgnoreCase)
                    || command.CommandText.StartsWith("update", StringComparison.InvariantCultureIgnoreCase)
                    || command.CommandText.StartsWith("delete", StringComparison.InvariantCultureIgnoreCase))
                {
                    ////阻止原数据库写入
                    //interceptionContext.SuppressExecution();
                    ////让ef返回成功,不抛异常
                    //interceptionContext.Result = 1;
                    //return;

                    //判断是否含有事务,理论上保存后所有操作ef会自动包含事务
                    if (command.Transaction == null)
                    {
                        //获取完整的sql
                        var sql = SqlHelper.GetFullCommand(command);
                        ActiveMqHelper.ConfigList.ForEach(item =>
                        {
                            var model = new MqData { Sql = new List<string> { sql } };
                            ActiveMqHelper.SendMessage(item, model);
                        });
                    }
                }

            base.NonQueryExecuting(command, interceptionContext);
        }
    }
}