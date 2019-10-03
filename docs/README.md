## Assumptions and Technical Decisions

- All DB operations will be capture in a MongoDB session
- Initialization
   - Customer will supply the `MongoClient`
      - A default `MongoClient` will initialize if none is specified: `mongo://localhost`
- Sagas
   - Collections will be named after the saga data type in lowercase
   - The version integer will be captured in a `_version` element in the BSON document in MongoDB
   - Serialization will not fail if the saga data type does not contain a property corresponding to an element in the stored BSON document   
- Backwards compatible with community persisters
   - No need for user to specify the version property
   - No need for user to change data in database for compatibility
      - Allow override of version property element name
      - Allow override of collection naming convention
