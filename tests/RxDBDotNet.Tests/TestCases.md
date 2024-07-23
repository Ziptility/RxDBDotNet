# RxDBDotNet E2E Test Cases

This document outlines a comprehensive set of end-to-end (E2E) test cases designed to validate the functionality, performance, and reliability of the RxDBDotNet library. These tests focus on the correct implementation of the RxDB replication protocol, integration with Hot Chocolate GraphQL, and the proper handling of Entity Framework Core (EF Core) in a distributed application context.

The test cases are organized to progress from basic operations to more complex scenarios, ensuring that fundamental functionalities are tested before moving on to advanced features. This approach allows for a systematic validation of the library's capabilities and its adherence to the RxDB replication protocol.

## Test Cases

### 1. Basic Document Operations

#### Test Case 1.1: Create a Single Document

**Objective:** Verify that a single document can be created and stored correctly using the RxDBDotNet library.

**Preconditions:**
- RxDBDotNet server is running and accessible
- GraphQL endpoint is configured and operational

**Data Setup:**
- Workspace document data:
  ```csharp
  var newWorkspace = new Workspace
  {
      Id = Guid.NewGuid(),
      Name = "Test Workspace",
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```

**Execution Flow:**
1. Send a GraphQL mutation to create the new workspace document
2. Retrieve the created document using a GraphQL query

**Expected Results:**
- The mutation should return a success response
- The query should return a document matching the input data
- The document should be present in the underlying database

**Additional Notes:**
- Verify that the UpdatedAt field is set to the current time
- Ensure that the IsDeleted field is set to false

#### Test Case 1.2: Retrieve a Single Document

**Objective:** Confirm that a previously created document can be retrieved accurately.

**Preconditions:**
- A workspace document exists in the system (created in Test Case 1.1)

**Execution Flow:**
1. Send a GraphQL query to retrieve the workspace document by its ID

**Expected Results:**
- The query should return the correct workspace document
- All fields should match the data from the creation step

#### Test Case 1.3: Update a Single Document

**Objective:** Verify that an existing document can be updated correctly.

**Preconditions:**
- A workspace document exists in the system (created in Test Case 1.1)

**Data Setup:**
- Updated workspace data:
  ```csharp
  var updatedWorkspace = new Workspace
  {
      Id = [Existing Workspace Id],
      Name = "Updated Workspace Name",
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```

**Execution Flow:**
1. Send a GraphQL mutation to update the existing workspace document
2. Retrieve the updated document using a GraphQL query

**Expected Results:**
- The mutation should return a success response
- The query should return the document with updated fields
- The UpdatedAt field should be more recent than the original creation time

#### Test Case 1.4: Soft Delete a Document

**Objective:** Ensure that the soft delete functionality works as expected.

**Preconditions:**
- A workspace document exists in the system (created in Test Case 1.1)

**Execution Flow:**
1. Send a GraphQL mutation to soft delete the workspace document
2. Retrieve the soft-deleted document using a GraphQL query

**Expected Results:**
- The mutation should return a success response
- The query should return the document with IsDeleted set to true
- The document should still be present in the database, not physically deleted

### 2. RxDB Replication Protocol Implementation

#### Test Case 2.1: Initial Pull (Checkpoint Iteration)

**Objective:** Verify the correct implementation of the initial pull operation with a null checkpoint.

**Preconditions:**
- Multiple documents exist in the system

**Data Setup:**
- Create at least 3 workspace documents with known content and timestamps:
  ```csharp
  var documents = new List<Workspace>
  {
      new Workspace
      {
          Id = Guid.NewGuid(),
          Name = "Workspace 1",
          UpdatedAt = DateTimeOffset.UtcNow.AddHours(-2),
          IsDeleted = false
      },
      new Workspace
      {
          Id = Guid.NewGuid(),
          Name = "Workspace 2",
          UpdatedAt = DateTimeOffset.UtcNow.AddHours(-1),
          IsDeleted = false
      },
      new Workspace
      {
          Id = Guid.NewGuid(),
          Name = "Workspace 3",
          UpdatedAt = DateTimeOffset.UtcNow,
          IsDeleted = false
      }
  };
  ```

**Execution Flow:**
1. Send a GraphQL query to pull documents with null checkpoint and a limit of 10

**Expected Results:**
- The query should return all documents, ordered by their UpdatedAt timestamp
- The response should include a new checkpoint based on the latest document
- The number of returned documents should not exceed the specified limit

**Additional Notes:**
- Verify that the documents are returned in the correct order (oldest to newest)
- Ensure that the checkpoint in the response matches the UpdatedAt and Id of the newest document

#### Test Case 2.2: Pull Documents with Filtering

**Objective:** Verify that documents can be pulled with filtering applied, ensuring that the filtering works correctly while adhering to the RxDB replication protocol.

**Preconditions:**
- Multiple documents of various types exist in the system
- The GraphQL server is configured with filtering support

**Data Setup:**
- Create at least 10 User documents with varied attributes:
  ```csharp
  var users = new List<User>
  {
      new User
      {
          Id = Guid.NewGuid(),
          FirstName = "John",
          LastName = "Doe",
          Email = "john.doe@example.com",
          Role = UserRole.User,
          WorkspaceId = workspaceId,
          UpdatedAt = DateTimeOffset.UtcNow.AddDays(-5),
          IsDeleted = false
      },
      new User
      {
          Id = Guid.NewGuid(),
          FirstName = "Jane",
          LastName = "Smith",
          Email = "jane.smith@example.com",
          Role = UserRole.Admin,
          WorkspaceId = workspaceId,
          UpdatedAt = DateTimeOffset.UtcNow.AddDays(-3),
          IsDeleted = false
      },
      // ... (add more users with varying attributes)
  };
  ```

**Execution Flow:**
1. Send a GraphQL query to pull User documents with the following filters:
   - `FirstName` contains "Jo"
   - `Role` is `User`
   - `UpdatedAt` is greater than 7 days ago
2. Set a limit of 5 documents
3. Use a null checkpoint (initial pull)

**GraphQL Query:**
```graphql
query {
  pullUser(
    checkpoint: null,
    limit: 5,
    where: {
      firstName: { contains: "Jo" },
      role: { eq: USER },
      updatedAt: { gt: "2023-07-11T00:00:00Z" }  # Assuming current date is 2023-07-18
    }
  ) {
    documents {
      id
      firstName
      lastName
      email
      role
      updatedAt
      isDeleted
    }
    checkpoint {
      lastDocumentId
      updatedAt
    }
  }
}
```

**Expected Results:**
- The query should return only User documents that match all the specified criteria:
  - First names containing "Jo"
  - Role is User
  - Updated within the last 7 days
- The number of returned documents should not exceed the specified limit (5)
- The returned documents should be ordered by their `UpdatedAt` timestamp (oldest to newest)
- The response should include a valid checkpoint based on the latest returned document
- Verify that the filtering does not interfere with the checkpoint mechanism of the RxDB replication protocol

**Additional Verifications:**
1. Perform a subsequent pull operation using the checkpoint from the first query:
   - It should only return documents that were updated after the checkpoint
   - The filtering should still be applied
2. Modify the filter to include deleted documents (`isDeleted: true`) and verify that soft-deleted documents are included when explicitly requested
3. Test with various combination of filters to ensure all filterable fields work correctly
4. Verify that invalid filter inputs are handled gracefully with appropriate error messages

**Execution Steps:**
1. Execute the initial filtered pull query
2. Verify the results match the expected criteria
3. Extract the checkpoint from the response
4. Create or update some documents that match the filter criteria
5. Execute a subsequent pull query using the checkpoint from step 3
6. Verify that only new or updated documents are returned, and they still match the filter criteria

**Additional Notes:**
- Ensure that the filtering logic is applied at the database level for efficiency
- Verify that the filtering does not break the RxDB replication protocol's consistency guarantees
- Check that the performance of filtered queries is acceptable, especially with large datasets

#### Test Case 2.3: Pull Documents with Projections and Filtering

**Objective:** Verify that documents can be pulled with both projections and filtering applied, ensuring that these features work correctly while adhering to the RxDB replication protocol.

**Preconditions:**
- Multiple documents of various types exist in the system
- The GraphQL server is configured with both projection and filtering support
- The middleware order is correct: UseProjections > UseFiltering

**Data Setup:**
- Create at least 10 User documents with varied attributes:
  ```csharp
  var users = new List<User>
  {
      new User
      {
          Id = Guid.NewGuid(),
          FirstName = "John",
          LastName = "Doe",
          Email = "john.doe@example.com",
          Role = UserRole.User,
          WorkspaceId = workspaceId,
          UpdatedAt = DateTimeOffset.UtcNow.AddDays(-5),
          IsDeleted = false
      },
      new User
      {
          Id = Guid.NewGuid(),
          FirstName = "Jane",
          LastName = "Smith",
          Email = "jane.smith@example.com",
          Role = UserRole.Admin,
          WorkspaceId = workspaceId,
          UpdatedAt = DateTimeOffset.UtcNow.AddDays(-3),
          IsDeleted = false
      },
      // ... (add more users with varying attributes)
  };
  ```

**Execution Flow:**
1. Send a GraphQL query to pull User documents with the following:
   - Projection: Include only `id`, `firstName`, and `email`
   - Filter: `Role` is `User`
   - Limit: 5 documents
   - Checkpoint: null (initial pull)

**GraphQL Query:**
```graphql
query {
  pullUser(
    checkpoint: null,
    limit: 5,
    where: { role: { eq: USER } }
  ) {
    documents {
      id
      firstName
      email
    }
    checkpoint {
      lastDocumentId
      updatedAt
    }
  }
}
```

**Expected Results:**
- The query should return User documents that match the specified criteria:
  - Role is User
- The returned documents should only include the projected fields: `id`, `firstName`, and `email`
- Other fields like `lastName`, `role`, `updatedAt`, and `isDeleted` should not be present in the response
- The number of returned documents should not exceed the specified limit (5)
- The response should include a valid checkpoint based on the latest returned document
- Verify that the projection and filtering do not interfere with the checkpoint mechanism of the RxDB replication protocol

**Additional Verifications:**
1. Perform a subsequent pull operation using the checkpoint from the first query:
   - It should only return documents that were updated after the checkpoint
   - The projection and filtering should still be applied
2. Test with various combinations of projections and filters to ensure all fields can be projected and filtered correctly
3. Verify that requesting non-existent fields in the projection is handled gracefully with appropriate error messages
4. Check that the `UpdatedAt` field is always included in the actual database query, even if not projected, to ensure proper ordering and checkpoint functionality

**Execution Steps:**
1. Execute the initial projected and filtered pull query
2. Verify the results match the expected criteria and only include the projected fields
3. Extract the checkpoint from the response
4. Create or update some documents that match the filter criteria
5. Execute a subsequent pull query using the checkpoint from step 3
6. Verify that only new or updated documents are returned, they still match the filter criteria, and only include the projected fields

**Additional Notes:**
- Ensure that the projection logic is applied at the database level for efficiency
- Verify that the projection does not break the RxDB replication protocol's consistency guarantees
- Check that the performance of projected queries is acceptable, especially with large datasets
- Confirm that the ordering by `UpdatedAt` and `Id` is maintained even when these fields are not projected

#### Test Case 2.4: Subsequent Pull (Checkpoint Iteration)

**Objective:** Ensure proper handling of pulls with a valid checkpoint.

**Preconditions:**
- Multiple documents exist in the system, including some created after the checkpoint from Test Case 2.1

**Data Setup:**
- Use the documents from Test Case 2.1
- Create a new document after recording the checkpoint from the previous pull:
  ```csharp
  var newDocument = new Workspace
  {
      Id = Guid.NewGuid(),
      Name = "Workspace 4",
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```

**Execution Flow:**
1. Send a GraphQL query to pull documents with the checkpoint from Test Case 2.1 and a limit of 10

**Expected Results:**
- The query should return only the documents created or updated after the given checkpoint
- The response should include a new checkpoint based on the latest document
- Documents created before or at the checkpoint time should not be included

**Additional Notes:**
- Verify that documents created before or at the checkpoint time are not included in the response
- Ensure that the new checkpoint in the response matches the UpdatedAt and Id of the newest document

#### Test Case 2.5: Checkpoint Iteration to Pull a Large Number of Documents

**Objective:** Verify correct implementation of checkpoint iteration for retrieving a large dataset.

**Preconditions:**
- A large number of documents (e.g., 250) exist in the system

**Data Setup:**
- Create 250 workspace documents with incremental UpdatedAt timestamps

**Execution Flow:**
1. Send an initial GraphQL query to pull documents with null checkpoint and a limit of 100
2. Record the returned checkpoint
3. Send a second query using the checkpoint from step 2 and the same limit
4. Record the new checkpoint
5. Send a third query using the checkpoint from step 4 and the same limit

**Expected Results:**
- The first query should return 100 documents and a checkpoint
- The second query should return 100 different documents and a new checkpoint
- The third query should return the remaining 50 documents and a final checkpoint
- Documents in each batch should be newer than those in the previous batch
- The final query should return an empty array of documents, indicating no more data to pull

**Additional Notes:**
- Verify that each batch contains the correct documents based on their UpdatedAt timestamps
- Ensure that no documents are missed or duplicated across batches
- Check that the checkpoint in each response correctly represents the last document in that batch

#### Test Case 2.6: Push New and Updated Documents in Single Operation

**Objective:** Verify correct handling of pushing new and updated documents to the server.

**Preconditions:**
- RxDBDotNet server is running and accessible

**Data Setup:**
- New document data:
  ```csharp
  var newUser = new User
  {
      Id = Guid.NewGuid(),
      FirstName = "John",
      LastName = "Doe",
      Email = "john.doe@example.com",
      Role = UserRole.User,
      WorkspaceId = [Existing Workspace Id],
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```
- Updated document data (assuming an existing user):
  ```csharp
  var updatedUser = new User
  {
      Id = [Existing User Id],
      FirstName = "Jane",
      LastName = "Doe",
      Email = "jane.doe@example.com",
      Role = UserRole.User,
      WorkspaceId = [Existing Workspace Id],
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```

**Execution Flow:**
1. Send a GraphQL mutation to push the new user document
2. Send another GraphQL mutation to push the updated user document
3. Retrieve both documents using GraphQL queries

**Expected Results:**
- Both push mutations should return success responses
- The retrieved documents should match the pushed data
- The server should correctly handle both the creation of a new document and the update of an existing one

#### Test Case 2.7: Conflict Detection and Resolution

**Objective:** Ensure that the server correctly detects and reports conflicts during push operations.

**Preconditions:**
- An existing user document in the system

**Data Setup:**
- Two conflicting updates to the same user document:
  ```csharp
  var update1 = new User
  {
      Id = [Existing User Id],
      FirstName = "Alice",
      LastName = "Smith",
      Email = "alice.smith@example.com",
      Role = UserRole.User,
      WorkspaceId = [Existing Workspace Id],
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };

  var update2 = new User
  {
      Id = [Existing User Id],
      FirstName = "Bob",
      LastName = "Johnson",
      Email = "bob.johnson@example.com",
      Role = UserRole.User,
      WorkspaceId = [Existing Workspace Id],
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```

**Execution Flow:**
1. Send a GraphQL mutation to push update1
2. Immediately after, send another GraphQL mutation to push update2 (using the original server state as assumedMasterState)
3. Attempt to retrieve the latest document state

**Expected Results:**
- The first push should succeed
- The second push should be rejected due to conflict
- The server should return conflict information
- A subsequent query should return the document with the first update applied

### 3. GraphQL and EF Core Integration

#### Test Case 3.1: Complex Query with Filtering and Pagination

**Objective:** Verify that complex GraphQL queries are correctly translated to EF Core queries.

**Preconditions:**
- Multiple user and workspace documents exist in the system

**Execution Flow:**
1. Send a GraphQL query to retrieve users with specific criteria (e.g., by role, email domain) with pagination

**Expected Results:**
- The query should return the correct set of users based on the specified criteria
- The results should be properly paginated
- The query should be efficiently executed at the database level (verify with query plan if possible)

#### Test Case 3.2: Nested Document Queries

**Objective:** Ensure that queries for nested or related documents are handled correctly.

**Preconditions:**
- Workspaces with associated users exist in the system

**Execution Flow:**
1. Send a GraphQL query to retrieve workspaces along with their associated users

**Expected Results:**
- The query should return workspaces with their related user documents
- The nested data should be correctly structured in the GraphQL response
- The query should be efficiently executed, avoiding N+1 query problems

### 4. Authorization and Authentication

#### Test Case 4.1: Basic Authorization Check

**Objective:** Verify that basic authorization rules are enforced.

**Preconditions:**
- Users with different roles (User, Admin, SuperAdmin) exist in the system

**Execution Flow:**
1. Attempt to perform operations (e.g., create a workspace) with different user roles

**Expected Results:**
- Operations should be allowed or denied based on the user's role
- Unauthorized attempts should be rejected with appropriate error messages

#### Test Case 4.2: Cross-Workspace Access Control

**Objective:** Ensure that users cannot access or modify data from workspaces they don't belong to.

**Preconditions:**
- Multiple workspaces with associated users exist

**Execution Flow:**
1. Authenticate as a user from Workspace A
2. Attempt to retrieve or modify data from Workspace B

**Expected Results:**
- The server should reject attempts to access or modify data from other workspaces
- Appropriate error messages should be returned

#### Test Case 4.3: Admin Operations

**Objective:** Verify that admin users can perform privileged operations within their workspace.

**Preconditions:**
- Admin users exist in different workspaces

**Execution Flow:**
1. Authenticate as an admin user
2. Perform admin-only operations (e.g., create new users, modify workspace settings)

**Expected Results:**
- Admin operations should be successful within the admin's workspace
- The same operations should be rejected for non-admin users

### 5. Real-time Updates and Subscriptions

#### Test Case 5.1: Document Change Subscription

**Objective:** Verify that real-time updates are correctly propagated through GraphQL subscriptions.

**Preconditions:**
- GraphQL subscription endpoint is configured and operational

**Execution Flow:**
1. Start a subscription for document changes (e.g., user document updates)
2. Perform a series of mutations (create, update, delete) on user documents
3. Observe the subscription stream

**Expected Results:**
- The subscription should receive real-time updates for each document change
- Updates should include the full updated document state
- The subscription should handle different types of changes (creation, update, deletion) correctly

#### Test Case 5.2: Filtered Subscriptions

**Objective:** Ensure that subscriptions can be filtered to receive only relevant updates.

**Preconditions:**
- Multiple types of documents (e.g., users, workspaces) exist in the system

**Execution Flow:**
1. Start a filtered subscription (e.g., only for workspace updates)
2. Perform mutations on various document types

**Expected Results:**
- The subscription should only receive updates for the specified document type
- Updates for other document types should not be received

### 6. Performance and Scalability

#### Test Case 6.1: Large Dataset Handling

**Objective:** Verify that the system can handle large datasets efficiently.

**Preconditions:**
- A large number of documents (e.g., 10,000+ users across multiple workspaces) exist in the system

**Execution Flow:**
1. Perform various operations (queries, mutations, subscriptions) on the large dataset

**Expected Results:**
- Operations should complete within acceptable time limits
- The system should not experience significant performance degradation
- Memory usage should remain within acceptable bounds

#### Test Case 6.2: Concurrent Operations

**Objective:** Ensure that the system can handle multiple concurrent operations without issues.

**Execution Flow:**
1. Simulate multiple clients performing various operations (queries, mutations, subscriptions) concurrently

**Expected Results:**
- The system should handle concurrent operations without errors
- Data consistency should be maintained across all operations
- Performance should not degrade significantly under concurrent load

### 7. Error Handling and Edge Cases

#### Test Case 7.1: Invalid Data Handling

**Objective:** Verify that the system properly handles and reports attempts to insert or update invalid data.

**Execution Flow:**
1. Attempt to create or update documents with invalid data (e.g., missing required fields, incorrect data types)

**Expected Results:**
- The system should reject invalid data with appropriate error messages
- The database should not be polluted with invalid data

#### Test Case 7.2: Network Interruption Handling

**Objective:** Ensure that the system gracefully handles network interruptions during replication.

**Execution Flow:**
1. Start a long-running operation (e.g., pulling a large dataset)
2. Simulate a network interruption
3. Restore the network connection and attempt to resume the operation

**Expected Results:**
- The system should detect the network interruption
- Upon network restoration, the operation should either resume or restart cleanly
- Data consistency should be maintained

These test cases provide a comprehensive validation of the RxDBDotNet library's functionality, focusing on its implementation of the RxDB replication protocol, integration with GraphQL and EF Core, and handling of various operational scenarios. By progressing from basic operations to more complex scenarios, the test suite ensures thorough coverage of the library's capabilities and its behavior under different conditions.