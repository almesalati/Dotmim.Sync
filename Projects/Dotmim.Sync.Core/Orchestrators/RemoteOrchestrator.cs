﻿using Dotmim.Sync.Batch;
using Dotmim.Sync.Builders;
using Dotmim.Sync.Enumerations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
    public partial class RemoteOrchestrator : BaseOrchestrator
    {

        /// <summary>
        /// Gets the sync side of this Orchestrator. RemoteOrchestrator is always used on server side
        /// </summary>
        public override SyncSide Side => SyncSide.ServerSide;


        /// <summary>
        /// Create a remote orchestrator, used to orchestrates the whole sync on the server side
        /// </summary>
        public RemoteOrchestrator(CoreProvider provider, SyncOptions options) : base(provider, options)
        {
            if (!this.Provider.CanBeServerProvider)
                throw new UnsupportedServerProviderException(this.Provider.GetProviderTypeName());
        }

        /// <summary>
        /// Ensure the schema is readed from the server, based on the Setup instance.
        /// Creates all required tables (server_scope tables) and provision all tables (tracking, stored proc, triggers and so on...)
        /// Then return the schema readed
        /// </summary>
        /// <returns>current context, the local scope info created or get from the database and the configuration from the client if changed </returns>
        //internal virtual async Task<ServerScopeInfo> EnsureSchemaAsync(DbConnection connection = default, DbTransaction transaction = default, CancellationToken cancellationToken = default, IProgress<ProgressArgs> progress = null)
        //{
        //    try
        //    {
        //        await using var runner = await this.GetConnectionAsync(scopeName, SyncMode.Writing, SyncStage.ScopeLoading, connection, transaction, cancellationToken, progress).ConfigureAwait(false);

        //        var exists = await this.InternalExistsScopeInfoTableAsync(this.GetContext(), DbScopeType.Server, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);
        //        if (!exists)
        //            await this.InternalCreateScopeInfoTableAsync(this.GetContext(), DbScopeType.Server, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

        //        exists = await this.InternalExistsScopeInfoTableAsync(this.GetContext(), DbScopeType.ServerHistory, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);
        //        if (!exists)
        //            await this.InternalCreateScopeInfoTableAsync(this.GetContext(), DbScopeType.ServerHistory, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

        //        var serverScopeInfo = await this.InternalGetScopeAsync<ServerScopeInfo>(this.GetContext(), DbScopeType.Server, this.ScopeName, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

        //        SyncSet schema;
        //        // Let's compare this serverScopeInfo with the current Setup
        //        // If schema is null :
        //        // - Read the schema from database based on this.Setup
        //        // - Provision the database with this schema
        //        // - Write the scope with this.Setup and schema
        //        // If schema is not null :
        //        // - Compare saved setup with current setup
        //        // - If not equals:
        //        // - Read schema from database based on this.Setup
        //        if (serverScopeInfo.Schema == null)
        //        {
        //            // So far, we don't have already a database provisionned
        //            this.GetContext().SyncStage = SyncStage.Provisioning;

        //            // 1) Get Schema from remote provider
        //            schema = await this.InternalGetSchemaAsync(this.GetContext(), this.Setup, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

        //            // 2) Provision
        //            var provision = SyncProvision.TrackingTable | SyncProvision.StoredProcedures | SyncProvision.Triggers;
        //            schema = await InternalProvisionAsync(this.GetContext(), false, schema, this.Setup, provision, serverScopeInfo, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);
        //        }
        //        else
        //        {
        //            // Setup stored on local or remote is different from the one provided.
        //            // So, we can migrate
        //            if (!serverScopeInfo.Setup.EqualsByProperties(this.Setup))
        //            {
        //                // 1) Get Schema from remote provider
        //                schema = await this.InternalGetSchemaAsync(this.GetContext(), this.Setup, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

        //                // Migrate the old setup (serverScopeInfo.Setup) to the new setup (this.Setup) based on the new schema 
        //                await this.InternalMigrationAsync(this.GetContext(), schema, serverScopeInfo.Setup, this.Setup, runner.Connection, runner.Transaction, cancellationToken, progress);

        //                serverScopeInfo.Setup = this.Setup;
        //                serverScopeInfo.Schema = schema;

        //                // Write scopes locally
        //                await this.InternalSaveScopeAsync(this.GetContext(), DbScopeType.Server, serverScopeInfo, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);
        //            }

        //            // Get the schema saved on server
        //            schema = serverScopeInfo.Schema;
        //        }

        //        await runner.CommitAsync().ConfigureAwait(false);

        //        return serverScopeInfo;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw GetSyncError(ex);
        //    }
        //}


        /// <summary>
        /// Migrate an old setup configuration to a new one. This method is usefull if you are changing your SyncSetup when a database has been already configured previously
        /// </summary>
        //public virtual async Task<ServerScopeInfo> MigrationAsync(SyncSetup oldSetup, DbConnection connection = default, DbTransaction transaction = default, CancellationToken cancellationToken = default, IProgress<ProgressArgs> progress = null)
        //{
        //    try
        //    {
        //        await using var runner = await this.GetConnectionAsync(scopeName, SyncMode.Writing, SyncStage.Migrating, connection, transaction, cancellationToken, progress).ConfigureAwait(false);

        //        SyncSet schema = await this.InternalGetSchemaAsync(this.GetContext(), this.Setup, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

        //        // Migrate the db structure
        //        await this.InternalMigrationAsync(this.GetContext(), schema, oldSetup, this.Setup, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

        //        var exists = await this.InternalExistsScopeInfoTableAsync(this.GetContext(), DbScopeType.Server, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

        //        if (!exists)
        //            await this.InternalCreateScopeInfoTableAsync(this.GetContext(), DbScopeType.Server, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

        //        var remoteScope = await this.InternalGetScopeAsync<ServerScopeInfo>(this.GetContext(), DbScopeType.Server, this.ScopeName, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

        //        if (remoteScope == null)
        //            remoteScope = await this.InternalCreateScopeAsync<ServerScopeInfo>(this.GetContext(), DbScopeType.Server, this.ScopeName, connection, transaction, cancellationToken, progress).ConfigureAwait(false);

        //        remoteScope.Setup = this.Setup;
        //        remoteScope.Schema = schema;

        //        // Write scopes locally
        //        await this.InternalSaveScopeAsync(this.GetContext(), DbScopeType.Server, remoteScope, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

        //        await runner.CommitAsync().ConfigureAwait(false);

        //        return remoteScope;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw GetSyncError(ex);
        //    }
        //}


        /// <summary>
        /// Apply changes on remote provider
        /// </summary>
        internal virtual async Task<(long RemoteClientTimestamp, BatchInfo ServerBatchInfo, ConflictResolutionPolicy ServerPolicy, DatabaseChangesApplied ClientChangesApplied, DatabaseChangesSelected ServerChangesSelected)>
            ApplyThenGetChangesAsync(ScopeInfo clientScope, BatchInfo clientBatchInfo, DbConnection connection = default, DbTransaction transaction = default, CancellationToken cancellationToken = default, IProgress<ProgressArgs> progress = null)
        {
            try
            {
                if (!this.StartTime.HasValue)
                    this.StartTime = DateTime.UtcNow;

                // Get context or create a new one
                var ctx = this.GetContext(clientScope.Name);

                long remoteClientTimestamp = 0L;
                DatabaseChangesSelected serverChangesSelected = null;
                DatabaseChangesApplied clientChangesApplied = null;
                BatchInfo serverBatchInfo = null;
                SyncSet schema = null;


                //Direction set to Upload
                ctx.SyncWay = SyncWay.Upload;

                IScopeInfo serverClientScopeInfo = null;
                // Create two transactions
                // First one to commit changes
                // Second one to get changes now that everything is commited
                await using (var runner = await this.GetConnectionAsync(clientScope.Name, SyncMode.Writing, SyncStage.ChangesApplying, connection, transaction, cancellationToken, progress).ConfigureAwait(false))
                {
                    // Getting server scope assumes we have already created the schema on server
                    // Scope name is the scope name coming from client
                    // Since server can have multiples scopes
                    serverClientScopeInfo = await this.InternalGetScopeAsync(clientScope.Name, DbScopeType.Server, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                    // Should we ?
                    if (serverClientScopeInfo == null || serverClientScopeInfo.Schema == null)
                        throw new MissingRemoteOrchestratorSchemaException();

                    // Deserialiaze schema
                    schema = serverClientScopeInfo.Schema;

                    // Create message containing everything we need to apply on server side
                    var applyChanges = new MessageApplyChanges(Guid.Empty, clientScope.Id, false, clientScope.LastServerSyncTimestamp, schema, this.Options.ConflictResolutionPolicy,
                                    this.Options.DisableConstraintsOnApplyChanges, this.Options.CleanMetadatas, this.Options.CleanFolder, false, clientBatchInfo, this.Options.LocalSerializerFactory);

                    // Call provider to apply changes
                    (ctx, clientChangesApplied) = await this.InternalApplyChangesAsync(serverClientScopeInfo, applyChanges, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                    await this.InterceptAsync(new TransactionCommitArgs(ctx, runner.Connection, runner.Transaction), progress, cancellationToken).ConfigureAwait(false);

                    // commit first transaction
                    await runner.CommitAsync().ConfigureAwait(false);
                }

                await using (var runner = await this.GetConnectionAsync(clientScope.Name, SyncMode.Reading, SyncStage.ChangesSelecting, connection, transaction, cancellationToken, progress).ConfigureAwait(false))
                {
                    ctx.ProgressPercentage = 0.55;

                    //Direction set to Download
                    ctx.SyncWay = SyncWay.Download;

                    // JUST Before get changes, get the timestamp, to be sure to 
                    // get rows inserted / updated elsewhere since the sync is not over
                    remoteClientTimestamp = await this.InternalGetLocalTimestampAsync(serverClientScopeInfo.Name, runner.Connection, runner.Transaction, cancellationToken, progress);

                    // Get if we need to get all rows from the datasource
                    var fromScratch = clientScope.IsNewScope || ctx.SyncType == SyncType.Reinitialize || ctx.SyncType == SyncType.ReinitializeWithUpload;

                    // When we get the chnages from server, we create the batches if it's requested by the client
                    // the batch decision comes from batchsize from client
                    (ctx, serverBatchInfo, serverChangesSelected) =
                        await this.InternalGetChangesAsync(clientScope, fromScratch, clientScope.LastServerSyncTimestamp, clientScope.Id,
                        this.Provider.SupportsMultipleActiveResultSets, null, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested)
                        cancellationToken.ThrowIfCancellationRequested();

                    // generate the new scope item
                    this.CompleteTime = DateTime.UtcNow;

                    var scopeHistory = new ServerHistoryScopeInfo
                    {
                        Id = clientScope.Id,
                        Name = clientScope.Name,
                        LastSyncTimestamp = remoteClientTimestamp,
                        LastSync = this.CompleteTime,
                        LastSyncDuration = this.CompleteTime.Value.Subtract(this.StartTime.Value).Ticks,
                    };

                    // Write scopes locally
                    await this.InternalSaveScopeAsync(scopeHistory, DbScopeType.ServerHistory, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                    // Commit second transaction for getting changes
                    await this.InterceptAsync(new TransactionCommitArgs(ctx, runner.Connection, runner.Transaction), progress, cancellationToken).ConfigureAwait(false);

                    await runner.CommitAsync().ConfigureAwait(false);
                }
                return (remoteClientTimestamp, serverBatchInfo, this.Options.ConflictResolutionPolicy, clientChangesApplied, serverChangesSelected);
            }
            catch (Exception ex)
            {
                throw GetSyncError(clientScope.Name, ex);
            }
        }

        /// <summary>
        /// Get changes from remote database
        /// </summary>
        public virtual async Task<(long RemoteClientTimestamp, BatchInfo ServerBatchInfo, DatabaseChangesSelected ServerChangesSelected)>
            GetChangesAsync(ScopeInfo clientScope, DbConnection connection = default, DbTransaction transaction = default, CancellationToken cancellationToken = default, IProgress<ProgressArgs> progress = null)
        {
            try
            {
                // Get context or create a new one
                var ctx = this.GetContext(clientScope.Name);

                await using var runner = await this.GetConnectionAsync(clientScope.Name, SyncMode.Reading, SyncStage.ChangesSelecting, connection, transaction, cancellationToken, progress).ConfigureAwait(false);

                // Before getting changes, be sure we have a remote schema available
                var serverScope = await this.GetServerScopeAsync(clientScope.Name, clientScope.Setup, runner.Connection, runner.Transaction, cancellationToken, progress);
                // TODO : if serverScope.Schema is null, should we Provision here ?

                // Should we ?
                if (serverScope.Schema == null)
                    throw new MissingRemoteOrchestratorSchemaException();


                //Direction set to Download
                ctx.SyncWay = SyncWay.Download;

                // Output
                // JUST Before get changes, get the timestamp, to be sure to 
                // get rows inserted / updated elsewhere since the sync is not over
                var remoteClientTimestamp = await this.InternalGetLocalTimestampAsync(clientScope.Name, runner.Connection, runner.Transaction, cancellationToken, progress);

                // Get if we need to get all rows from the datasource
                var fromScratch = clientScope.IsNewScope || ctx.SyncType == SyncType.Reinitialize || ctx.SyncType == SyncType.ReinitializeWithUpload;

                BatchInfo serverBatchInfo;
                DatabaseChangesSelected serverChangesSelected;
                // When we get the chnages from server, we create the batches if it's requested by the client
                // the batch decision comes from batchsize from client
                (ctx, serverBatchInfo, serverChangesSelected) =
                    await this.InternalGetChangesAsync(clientScope, fromScratch, clientScope.LastServerSyncTimestamp, 
                    clientScope.Id, this.Provider.SupportsMultipleActiveResultSets, null, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                await runner.CommitAsync().ConfigureAwait(false);

                return (remoteClientTimestamp, serverBatchInfo, serverChangesSelected);
            }
            catch (Exception ex)
            {
                throw GetSyncError(clientScope.Name, ex);
            }

        }

        /// <summary>
        /// Get estimated changes from remote database to be applied on client
        /// </summary>
        public virtual async Task<(long RemoteClientTimestamp, DatabaseChangesSelected ServerChangesSelected)>
                    GetEstimatedChangesCountAsync(ScopeInfo clientScope, DbConnection connection = default, DbTransaction transaction = default, CancellationToken cancellationToken = default, IProgress<ProgressArgs> progress = null)
        {
            try
            {

                var serverScope = await this.GetServerScopeAsync(clientScope.Name, clientScope.Setup, connection, transaction, cancellationToken, progress); ;

                // Should we ?
                if (serverScope.Schema == null)
                    throw new MissingRemoteOrchestratorSchemaException();

                await using var runner = await this.GetConnectionAsync(clientScope.Name, SyncMode.Reading, SyncStage.ChangesSelecting, connection, transaction, cancellationToken, progress).ConfigureAwait(false);
                var ctx = this.GetContext(clientScope.Name);
                ctx.SyncStage = SyncStage.ChangesSelecting;
                //Direction set to Download
                ctx.SyncWay = SyncWay.Download;

                // Output
                // JUST Before get changes, get the timestamp, to be sure to 
                // get rows inserted / updated elsewhere since the sync is not over
                var remoteClientTimestamp = await this.InternalGetLocalTimestampAsync(clientScope.Name, runner.Connection, runner.Transaction, cancellationToken, progress);

                // Get if we need to get all rows from the datasource
                var fromScratch = clientScope.IsNewScope || ctx.SyncType == SyncType.Reinitialize || ctx.SyncType == SyncType.ReinitializeWithUpload;

                DatabaseChangesSelected serverChangesSelected;
                // When we get the chnages from server, we create the batches if it's requested by the client
                // the batch decision comes from batchsize from client
                (ctx, serverChangesSelected) =
                    await this.InternalGetEstimatedChangesCountAsync(clientScope, fromScratch, clientScope.LastServerSyncTimestamp, clientScope.Id, this.Provider.SupportsMultipleActiveResultSets, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                return (remoteClientTimestamp, serverChangesSelected);
            }
            catch (Exception ex)
            {
                throw GetSyncError(clientScope.Name, ex);
            }
        }

        /// <summary>
        /// Delete all metadatas from tracking tables, based on min timestamp from history client table
        /// </summary>
        public async Task<DatabaseMetadatasCleaned> DeleteMetadatasAsync(string scopeName, DbConnection connection = default, DbTransaction transaction = default, CancellationToken cancellationToken = default, IProgress<ProgressArgs> progress = null)
        {
            // Get the min timestamp, where we can without any problem, delete metadatas
            var histories = await this.GetServerHistoryScopesAsync(scopeName, connection, transaction, cancellationToken, progress);

            if (histories == null || histories.Count == 0)
                return new DatabaseMetadatasCleaned();

            var minTimestamp = histories.Min(shsi => shsi.LastSyncTimestamp);

            if (minTimestamp == 0)
                return new DatabaseMetadatasCleaned();

            return await this.DeleteMetadatasAsync(scopeName, minTimestamp, connection, transaction, cancellationToken, progress);
        }

        /// <summary>
        /// Delete metadatas items from tracking tables
        /// </summary>
        /// <param name="timeStampStart">Timestamp start. Used to limit the delete metadatas rows from now to this timestamp</param>
        public override async Task<DatabaseMetadatasCleaned> DeleteMetadatasAsync(string scopeName, long? timeStampStart, DbConnection connection = default, DbTransaction transaction = default, CancellationToken cancellationToken = default, IProgress<ProgressArgs> progress = null)
        {
            try
            {
                if (!timeStampStart.HasValue)
                    return null;

                await using var runner = await this.GetConnectionAsync(SyncOptions.DefaultScopeName, SyncMode.Writing, SyncStage.MetadataCleaning, connection, transaction, cancellationToken, progress).ConfigureAwait(false);
                
                var ctx = this.GetContext(SyncOptions.DefaultScopeName);

                var serverScopeInfo = await this.InternalGetScopeAsync(scopeName, DbScopeType.Server, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false) as ServerScopeInfo;

                await this.InterceptAsync(new MetadataCleaningArgs(ctx, serverScopeInfo.Setup, timeStampStart.Value, runner.Connection, runner.Transaction), progress, cancellationToken).ConfigureAwait(false);

                var databaseMetadatasCleaned = await this.InternalDeleteMetadatasAsync(serverScopeInfo, timeStampStart.Value, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                serverScopeInfo.LastCleanupTimestamp = databaseMetadatasCleaned.TimestampLimit;

                await this.InternalSaveScopeAsync(serverScopeInfo, DbScopeType.Server, runner.Connection, runner.Transaction, cancellationToken, progress).ConfigureAwait(false);

                await this.InterceptAsync(new MetadataCleanedArgs(ctx, databaseMetadatasCleaned, runner.Connection), progress, cancellationToken).ConfigureAwait(false);

                await runner.CommitAsync().ConfigureAwait(false);

                return databaseMetadatasCleaned;

            }
            catch (Exception ex)
            {
                throw GetSyncError(scopeName, ex);
            }
        }

    }
}