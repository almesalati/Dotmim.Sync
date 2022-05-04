﻿using Dotmim.Sync.Args;
using Dotmim.Sync.Batch;
using Dotmim.Sync.Builders;
using Dotmim.Sync.Enumerations;
using Dotmim.Sync.Manager;
using Dotmim.Sync.Serialization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dotmim.Sync
{
    public abstract partial class BaseOrchestrator
    {

        internal virtual async Task<DatabaseMetadatasCleaned> InternalDeleteMetadatasAsync(
                    IEnumerable<IScopeInfo> scopeInfos, long timestampLimit,
                    DbConnection connection, DbTransaction transaction,
                    CancellationToken cancellationToken, IProgress<ProgressArgs> progress)
        {

            var context = this.GetContext(SyncOptions.DefaultScopeName);

            DatabaseMetadatasCleaned databaseMetadatasCleaned = new DatabaseMetadatasCleaned { TimestampLimit = timestampLimit };

            await this.InterceptAsync(new MetadataCleaningArgs(context, scopeInfos, timestampLimit, 
                                        connection, transaction), progress, cancellationToken).ConfigureAwait(false);

            // contains all tables already processed
            var doneList = new List<SetupTable>();

            foreach (var scopeInfo in scopeInfos)
            {
                foreach (var setupTable in scopeInfo.Setup.Tables)
                {
                    var isDone = doneList.Any(t => t.EqualsByName(setupTable));

                    if (isDone)
                        continue;

                    // create a fake syncTable
                    // Don't need anything else than table name to make a delete metadata clean up
                    var syncTable = new SyncTable(setupTable.TableName, setupTable.SchemaName);

                    // Create sync adapter
                    var syncAdapter = this.GetSyncAdapter(syncTable, scopeInfo);

                    var (command, _) = await syncAdapter.GetCommandAsync(DbCommandType.DeleteMetadata, connection, transaction);

                    if (command != null)
                    {
                        // Set the special parameters for delete metadata
                        DbSyncAdapter.SetParameterValue(command, "sync_row_timestamp", timestampLimit);

                        await this.InterceptAsync(new DbCommandArgs(context, command, connection, transaction), progress, cancellationToken).ConfigureAwait(false);

                        var rowsCleanedCount = await command.ExecuteNonQueryAsync().ConfigureAwait(false);

                        // Check if we have a return value instead
                        var syncRowCountParam = DbSyncAdapter.GetParameter(command, "sync_row_count");

                        if (syncRowCountParam != null)
                            rowsCleanedCount = (int)syncRowCountParam.Value;

                        // Only add a new table metadata stats object, if we have, at least, purged 1 or more rows
                        if (rowsCleanedCount > 0)
                        {
                            var tableMetadatasCleaned = new TableMetadatasCleaned(syncTable.TableName, syncTable.SchemaName)
                            {
                                RowsCleanedCount = rowsCleanedCount,
                                TimestampLimit = timestampLimit
                            };

                            databaseMetadatasCleaned.Tables.Add(tableMetadatasCleaned);
                        }

                    }

                    doneList.Add(setupTable);
                }
            }

            await this.InterceptAsync(new MetadataCleanedArgs(context, databaseMetadatasCleaned, connection), progress, cancellationToken).ConfigureAwait(false);
            return databaseMetadatasCleaned;
        }



        /// <summary>
        /// Update a metadata row
        /// </summary>
        internal async Task<bool> InternalUpdateMetadatasAsync(IScopeInfo scopeInfo, DbSyncAdapter syncAdapter, SyncRow row, Guid? senderScopeId, bool forceWrite, DbConnection connection, DbTransaction transaction)
        {
            var (command, _) = await syncAdapter.GetCommandAsync(DbCommandType.UpdateMetadata, connection, transaction);

            if (command == null) return false;

            // Set the parameters value from row
            syncAdapter.SetColumnParametersValues(command, row);

            // Set the special parameters for update
            syncAdapter.AddScopeParametersValues(command, senderScopeId, 0, row.RowState == DataRowState.Deleted, forceWrite);

            var context = this.GetContext(scopeInfo.Name);

            await this.InterceptAsync(new DbCommandArgs(context, command, connection, transaction)).ConfigureAwait(false);

            var metadataUpdatedRowsCount = await command.ExecuteNonQueryAsync().ConfigureAwait(false);

            // Check if we have a return value instead
            var syncRowCountParam = DbSyncAdapter.GetParameter(command, "sync_row_count");

            if (syncRowCountParam != null)
                metadataUpdatedRowsCount = (int)syncRowCountParam.Value;

            return metadataUpdatedRowsCount > 0;
        }


    }
}
