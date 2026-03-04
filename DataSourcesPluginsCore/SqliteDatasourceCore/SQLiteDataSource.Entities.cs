namespace TheTechIdea.Beep.DataBase
{
    public partial class SQLiteDataSource
    {
        public override IEnumerable<string> GetEntitesList()
        {
            base.GetEntitesList();

            if (Dataconnection.InMemory)
            {
                foreach (var item in InMemoryStructures)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.EntityName))
                    {
                        continue;
                    }

                    if (!EntitiesNames.Contains(item.EntityName))
                    {
                        EntitiesNames.Add(item.EntityName);
                    }

                    if (!Entities.Any(e => e.EntityName.Equals(item.EntityName, StringComparison.OrdinalIgnoreCase)))
                    {
                        Entities.Add(item);
                    }
                }
            }

            return EntitiesNames;
        }

        public override bool CreateEntityAs(EntityStructure entity)
        {
            string ds = entity.DataSourceID;
            if (!Dataconnection.InMemory)
            {
                if (EntitiesNames.Contains(entity.EntityName))
                {
                    return false;
                }
                if (Entities.Any(c => c.EntityName == entity.EntityName))
                {
                    return false;
                }
            }

            bool retval = base.CreateEntityAs(entity);
            entity.DataSourceID = ds;
            if (retval)
            {
                entity.IsCreated = true;
                IsLoaded = true;
                IsCreated = true;
                IsStructureCreated = true;
            }

            return retval;
        }

        private EntityStructure GetEntity(EntityStructure entity)
        {
            EntityStructure ent = new EntityStructure
            {
                DatasourceEntityName = entity.DatasourceEntityName,
                DataSourceID = entity.DataSourceID,
                DatabaseType = entity.DatabaseType,
                Caption = entity.Caption,
                Category = entity.Category,
                Fields = entity.Fields,
                PrimaryKeys = entity.PrimaryKeys,
                Relations = entity.Relations,
                OriginalEntityName = entity.OriginalEntityName,
                GuidID = Guid.NewGuid().ToString(),
                ViewID = entity.ViewID,
                Viewtype = entity.Viewtype,
                EntityName = entity.EntityName,
                SchemaOrOwnerOrDatabase = entity.SchemaOrOwnerOrDatabase
            };
            return ent;
        }
    }
}
