using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.EntityClient;
using System.Globalization;
using System.Linq;
using System.Reflection;
using EFIntercepterSync.Helper;
using EFIntercepterSync.Model;

namespace EFIntercepterSync
{
    //重写SaveChange
    public class SubSaveOnChange
    {
        //SubSavingChanges，语句大于1执行则包含事务操作,打包进队列
        public static void SavingChanges(object sender, EventArgs e)
        {
            if (ActiveMqHelper.ConfigList.Any())
            {
                var list = new List<string>();

                var conn = sender.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.Name == "Connection")
                    .Select(p => p.GetValue(sender, null))
                    .SingleOrDefault();
                var entityConn = (EntityConnection) conn;

                var translatorT =
                    sender.GetType().Assembly
                        .GetType("System.Data.Entity.Core.Mapping.Update.Internal.UpdateTranslator");

                var entityAdapterT =
                    sender.GetType().Assembly.GetType("System.Data.Entity.Core.EntityClient.Internal.EntityAdapter");
                var entityAdapter = Activator.CreateInstance(entityAdapterT, BindingFlags.Instance |
                                                                             BindingFlags.NonPublic |
                                                                             BindingFlags.Public,
                    null, new[] {sender}, CultureInfo.InvariantCulture);

                entityAdapterT.GetProperty("Connection")?.SetValue(entityAdapter, entityConn);

                var translator = Activator.CreateInstance(translatorT, BindingFlags.Instance |
                                                                       BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    new[] {entityAdapter}, CultureInfo.InvariantCulture);

                var produceCommands = translator.GetType().GetMethod(
                    "ProduceCommands", BindingFlags.NonPublic | BindingFlags.Instance);

                if (produceCommands != null)
                {
                    var commands = (IEnumerable<object>) produceCommands.Invoke(translator, null);

                    foreach (var cmd in commands)
                    {
                        var identifierValues = new Dictionary<int, object>();
                        var dbCommand =
                            (DbCommand) cmd.GetType()
                                .GetMethod("CreateCommand", BindingFlags.Instance | BindingFlags.NonPublic)
                                ?.Invoke(cmd, new object[] {identifierValues});

                        if (dbCommand != null)
                            if (dbCommand.CommandText.StartsWith("insert", StringComparison.InvariantCultureIgnoreCase)
                                || dbCommand.CommandText.StartsWith("update",
                                    StringComparison.InvariantCultureIgnoreCase)
                                || dbCommand.CommandText.StartsWith("delete",
                                    StringComparison.InvariantCultureIgnoreCase))
                            {
                                var commandText = SqlHelper.GetFullCommand(dbCommand);
                                list.Add(commandText);
                            }
                    }
                }

                if (ActiveMqHelper.ConfigList.Any() && list.Any())
                    ActiveMqHelper.ConfigList.ForEach(item =>
                    {
                        var model = new MqData {Sql = list};
                        ActiveMqHelper.SendMessage(item, model);
                    });
            }
        }
    }
}