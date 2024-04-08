using System;
using System.Collections.Generic;
using System.Text;

namespace DuckDBDataSourceCore
{
    enum DuckDbFunctions
    {
        CreateDatabase,
        CreateTable,
        CreateView,
        CreateFunction,
        CreateProcedure,
        CreateTrigger,
        CreateIndex,
        CreateSchema,
        CreateRole,
        CreateSequence,
        CreateType,
        CreateExtension,
        CreateForeignDataWrapper,
        CreateForeignServer,
        CreateUserMapping,
        CreateEventTrigger,
        CreatePolicy,
        CreatePublication,
        CreateSubscription,
        CreateRoleMembership,
        CreateTablespace,
        CreateDatabaseLink,
        CreateMaterializedView,
        CreateMaterializedViewLog,
        CreateMaterializedViewGroup
    }
}
