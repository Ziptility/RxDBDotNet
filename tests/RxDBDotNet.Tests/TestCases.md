# RxDBDotNet E2E Test Cases using the LiveDocs test API

LiveDocs is a real-time collaborative document editing GraphQL API built on RxDBDotNet. It enables users to create, edit, and share documents seamlessly across multiple devices and users, all while maintaining data integrity and ensuring a smooth, responsive experience.

This test case document outlines a comprehensive set of scenarios designed to validate the functionality, performance, and reliability of the RxDBDotNet library in a real-world application context. Using LiveDocs, a real-time collaborative document editing application, as a test bed, these test cases go beyond simple CRUD operations to explore the intricacies of multi-user interactions and device synchronization.

The test cases encompass a wide range of scenarios, including:

1. Verification of data consistency across multiple clients
2. Proper merging of offline changes upon reconnection
3. Handling of edge cases such as conflicting edits and intermittent connectivity
4. Complex multi-user scenarios with simultaneous edits
5. Simulated network failures and recovery

By executing these test cases, we aim to thoroughly validate the robustness and reliability of the RxDBDotNet library under demanding, real-world conditions. This comprehensive approach ensures that RxDBDotNet can effectively support applications requiring seamless real-time collaboration and data synchronization across multiple users and devices.

## Test Cases

### 0. Database Seeding

#### Test Case 0.1: Seed Initial Data

**Objective:** Verify that the database is correctly seeded with initial data, including a super admin user and the "LiveDocs" workspace.

**Preconditions:**
- The database is empty
- The seeding process is implemented

**Data Setup:**
- Super admin user data:
  ```csharp
  var superAdminUser = new User
  {
      Id = Guid.NewGuid(),
      FirstName = "Super",
      LastName = "Admin",
      Email = "superadmin@livedocs.com",
      Role = UserRole.SuperAdmin,
      WorkspaceId = [LiveDocs Workspace Id],
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```
- LiveDocs workspace data:
  ```csharp
  var liveDocsWorkspace = new Workspace
  {
      Id = Guid.NewGuid(),
      Name = "LiveDocs",
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```

**Execution Flow:**
1. Run the database seeding process
2. Query the database for the seeded super admin user
3. Query the database for the seeded LiveDocs workspace

**Expected Results:**
- The super admin user should be present in the database with the correct data
- The LiveDocs workspace should be present in the database with the correct data
- The super admin user should be associated with the LiveDocs workspace

### 1. Workspace Management

#### Test Case 1.1: Create a new workspace

**Objective:** Verify that a new workspace can be created in the system.

**Preconditions:**
- RxDBDotNet server is running and accessible
- A super admin user exists in the system

**Data Setup:**
- New workspace data:
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
1. Authenticate as the super admin user
2. Send a GraphQL mutation to create the new workspace
3. Retrieve the created workspace using a GraphQL query

**Expected Results:**
- The mutation should return a success response
- The query should return a workspace object matching the input data
- The workspace should be present in the database

**Additional Notes:**
- Verify that the UpdatedAt field is set to the current time
- Ensure that the IsDeleted field is set to false

#### Test Case 1.2: Attempt to create a workspace with a duplicate name

**Objective:** Verify that the system prevents the creation of workspaces with duplicate names.

**Preconditions:**
- RxDBDotNet server is running and accessible
- A workspace named "Test Workspace" already exists in the system

**Data Setup:**
- Duplicate workspace data:
  ```csharp
  var duplicateWorkspace = new Workspace
  {
      Id = Guid.NewGuid(),
      Name = "Test Workspace",
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```

**Execution Flow:**
1. Authenticate as the super admin user
2. Send a GraphQL mutation to create the duplicate workspace

**Expected Results:**
- The mutation should return an error response indicating that the workspace name is already in use
- No new workspace should be created in the database

### 2. User Management

#### Test Case 2.1: Create a new user

**Objective:** Verify that a new user can be created in the system and associated with a workspace.

**Preconditions:**
- RxDBDotNet server is running and accessible
- At least one workspace exists in the system

**Data Setup:**
- New user data:
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

**Execution Flow:**
1. Authenticate as an admin user
2. Send a GraphQL mutation to create the new user
3. Retrieve the created user using a GraphQL query

**Expected Results:**
- The mutation should return a success response
- The query should return a user object matching the input data
- The user should be present in the database and associated with the correct workspace

**Additional Notes:**
- Verify that the UpdatedAt field is set to the current time
- Ensure that the IsDeleted field is set to false
- Check that the WorkspaceId matches the specified workspace

#### Test Case 2.2: Retrieve users within a workspace

**Objective:** Verify that users can be retrieved within the context of their workspace.

**Preconditions:**
- RxDBDotNet server is running and accessible
- Multiple users exist in different workspaces

**Data Setup:**
- Use existing users and workspaces

**Execution Flow:**
1. Authenticate as an admin user of a specific workspace
2. Send a GraphQL query to retrieve all users in the admin's workspace

**Expected Results:**
- The query should return only the users associated with the admin's workspace
- Users from other workspaces should not be included in the results

### 3. Document Operations

#### Test Case 3.1: Create a new document within a workspace

**Objective:** Verify that a new document can be created in the system and associated with a specific workspace.

**Preconditions:**
- RxDBDotNet server is running and accessible
- At least one user and one workspace exist in the system

**Data Setup:**
- New document data:
  ```csharp
  var newDocument = new LiveDoc
  {
      Id = Guid.NewGuid(),
      Content = "This is a test document.",
      OwnerId = [Existing User Id],
      WorkspaceId = [Existing Workspace Id],
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```

**Execution Flow:**
1. Authenticate as a user within the target workspace
2. Send a GraphQL mutation to create the new document
3. Retrieve the created document using a GraphQL query

**Expected Results:**
- The mutation should return a success response
- The query should return a document object matching the input data
- The document should be present in the database and associated with the correct workspace and owner

**Additional Notes:**
- Verify that the UpdatedAt field is set to the current time
- Ensure that the IsDeleted field is set to false
- Check that the WorkspaceId and OwnerId match the specified workspace and user

#### Test Case 3.2: Attempt to access a document from a different workspace

**Objective:** Verify that users cannot access documents from workspaces they don't belong to.

**Preconditions:**
- RxDBDotNet server is running and accessible
- At least two workspaces exist, each with their own users and documents

**Data Setup:**
- Use existing workspaces, users, and documents

**Execution Flow:**
1. Authenticate as a user from Workspace A
2. Attempt to retrieve a document that belongs to Workspace B using a GraphQL query

**Expected Results:**
- The query should return an error or null result
- The user should not be able to access or view the document from the other workspace

### 4. Pull Operations

#### Test Case 4.1: Initial Pull (Checkpoint Iteration) within a Workspace

**Objective:** Verify the correct implementation of the initial pull operation with a null checkpoint within a specific workspace.

**Preconditions:**
- RxDBDotNet server is running and accessible
- Multiple documents exist in different workspaces

**Data Setup:**
- Create at least 3 documents in a specific workspace with known content and timestamps:
  ```csharp
  var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Test Workspace", UpdatedAt = DateTimeOffset.UtcNow, IsDeleted = false };
  var user = new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Email = "john@example.com", Role = UserRole.User, WorkspaceId = workspace.Id, UpdatedAt = DateTimeOffset.UtcNow, IsDeleted = false };
  var documents = new List<LiveDoc>
  {
      new LiveDoc
      {
          Id = Guid.NewGuid(),
          Content = "First document",
          OwnerId = user.Id,
          WorkspaceId = workspace.Id,
          UpdatedAt = DateTimeOffset.UtcNow.AddHours(-2),
          IsDeleted = false
      },
      new LiveDoc
      {
          Id = Guid.NewGuid(),
          Content = "Second document",
          OwnerId = user.Id,
          WorkspaceId = workspace.Id,
          UpdatedAt = DateTimeOffset.UtcNow.AddHours(-1),
          IsDeleted = false
      },
      new LiveDoc
      {
          Id = Guid.NewGuid(),
          Content = "Third document",
          OwnerId = user.Id,
          WorkspaceId = workspace.Id,
          UpdatedAt = DateTimeOffset.UtcNow,
          IsDeleted = false
      }
  };
  ```

**Execution Flow:**
1. Authenticate as the user in the test workspace
2. Send a GraphQL query to pull documents with null checkpoint and a limit of 10

**Expected Results:**
- The query should return only the documents from the user's workspace, ordered by their UpdatedAt timestamp
- The response should include a new checkpoint based on the latest document in the workspace
- Documents from other workspaces should not be included in the results

**Additional Notes:**
- Verify that the documents are returned in the correct order (oldest to newest)
- Ensure that the checkpoint in the response matches the UpdatedAt and Id of the newest document in the workspace

#### Test Case 4.2: Subsequent Pull within a Workspace

**Objective:** Ensure proper handling of pulls with a valid checkpoint within a specific workspace.

**Preconditions:**
- RxDBDotNet server is running and accessible
- Multiple documents exist in different workspaces, including some created after the checkpoint in the test workspace

**Data Setup:**
- Use the documents from Test Case 4.1
- Create a new document in the test workspace after recording the checkpoint from the previous pull:
  ```csharp
  var newDocument = new LiveDoc
  {
      Id = Guid.NewGuid(),
      Content = "Fourth document",
      OwnerId = user.Id,
      WorkspaceId = workspace.Id,
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```

**Execution Flow:**
1. Authenticate as the user in the test workspace
2. Send a GraphQL query to pull documents with the checkpoint from Test Case 4.1 and a limit of 10

**Expected Results:**
- The query should return only the documents created or updated after the given checkpoint within the user's workspace
- In this case, it should return only the "Fourth document"
- The response should include a new checkpoint based on the latest document in the workspace
- Documents from other workspaces should not be included, even if they were created after the checkpoint

**Additional Notes:**
- Verify that documents created before or at the checkpoint time are not included in the response
- Ensure that the new checkpoint in the response matches the UpdatedAt and Id of the newest document in the workspace

#### Test Case 4.3: Checkpoint Iteration to Pull a Large Number of Documents in Small "Batches" within a Workspace

**Objective:** Verify correct implementation of checkpoint iteration operations using checkpoints and limits within a specific workspace.

**Preconditions:**
- RxDBDotNet server is running and accessible
- A large number of documents (e.g., 250) exist in the test workspace

**Data Setup:**
- Create 250 documents with incremental UpdatedAt timestamps in the test workspace

**Execution Flow:**
1. Authenticate as the user in the test workspace
2. Send an initial GraphQL query to pull documents with null checkpoint and a limit of 100
3. Record the returned checkpoint
4. Send a second query using the checkpoint from step 2 and the same limit
5. Record the new checkpoint
6. Send a third query using the checkpoint from step 4 and the same limit

**Expected Results:**
- The first query should return 100 documents from the user's workspace and a checkpoint
- The second query should return 100 different documents from the same workspace and a new checkpoint
- The third query should return the remaining 50 documents from the workspace and a final checkpoint
- Documents in each batch should be newer than those in the previous batch
- The final query should return an empty array of documents, indicating no more data to pull in the workspace

**Additional Notes:**
- Verify that each batch contains the correct documents based on their UpdatedAt timestamps
- Ensure that no documents are missed or duplicated across batches
- Check that the checkpoint in each response correctly represents the last document in that batch
- Confirm that no documents from other workspaces are included in any of the batches

### 5. Push Operations

#### Test Case 5.1: Push a New Document within a Workspace

**Objective:** Verify correct handling of pushing a new document to the server within a specific workspace.

**Preconditions:**
- RxDBDotNet server is running and accessible
- A user exists in a specific workspace

**Data Setup:**
- Existing user and workspace from previous test cases
- New document data:
  ```csharp
  var newDocument = new LiveDoc
  {
      Id = Guid.NewGuid(),
      Content = "This document was pushed from the client",
      OwnerId = user.Id,
      WorkspaceId = workspace.Id,
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```

**Execution Flow:**
1. Authenticate as the user in the test workspace
2. Send a GraphQL mutation to push the new document
3. Retrieve the pushed document using a GraphQL query

**Expected Results:**
- The mutation should return a success response
- The query should return a document matching the pushed data within the user's workspace
- The document should be present in the database with all fields matching the pushed data
- The document should be associated with the correct workspace

**Additional Notes:**
- Verify that the server doesn't modify any fields of the pushed document
- Ensure that a subsequent pull operation within the same workspace would include this new document
- Confirm that users from other workspaces cannot access this document

#### Test Case 5.2: Push an Updated Document within a Workspace

**Objective:** Ensure proper updating of existing documents through push operations within a specific workspace.

**Preconditions:**
- RxDBDotNet server is running and accessible
- A document exists in the user's workspace

**Data Setup:**
- Existing document ID from the user's workspace
- Updated document data:
  ```csharp
  var updatedDocument = new LiveDoc
  {
      Id = [Existing document ID],
      Content = "This document was updated from the client",
      OwnerId = user.Id,
      WorkspaceId = workspace.Id,
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```

**Execution Flow:**
1. Authenticate as the user in the test workspace
2. Retrieve the current state of the document
3. Send a GraphQL mutation to push the updated document, including the current server state as the assumedMasterState
4. Retrieve the updated document using a GraphQL query

**Expected Results:**
- The mutation should return a success response
- The query should return a document with the updated fields within the user's workspace
- The document in the database should reflect the changes and remain associated with the correct workspace

**Additional Notes:**
- Verify that unchanged fields remain the same
- Ensure that a subsequent pull operation within the same workspace would include this updated document
- Confirm that users from other workspaces cannot access or modify this document

### 6. Conflict Handling

#### Test Case 6.1: Detect and Report Conflict within a Workspace

**Objective:** Verify that conflicts are correctly detected and reported when pushing changes within a specific workspace.

**Preconditions:**
- RxDBDotNet server is running and accessible
- A document exists in the user's workspace

**Data Setup:**
- Existing document ID from the user's workspace
- Two client updates:
  ```csharp
  var clientUpdate1 = new LiveDoc
  {
      Id = [Existing document ID],
      Content = "Update from Client 1",
      OwnerId = user.Id,
      WorkspaceId = workspace.Id,
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };

  var clientUpdate2 = new LiveDoc
  {
      Id = [Existing document ID],
      Content = "Update from Client 2",
      OwnerId = user.Id,
      WorkspaceId = workspace.Id,
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```

**Execution Flow:**
1. Authenticate as the user in the test workspace
2. Both clients retrieve the current state of the document
3. Client 1 sends a push mutation with its update
4. Immediately after, Client 2 sends a push mutation with its update (using the original server state as assumedMasterState)
5. Both clients attempt to pull the latest document state

**Expected Results:**
- Client 1's push should succeed
- Client 2's push should be rejected due to conflict
- The pull operation should return the document with Client 1's update
- Client 2 should receive conflict information
- All operations should be contained within the same workspace

**Additional Notes:**
- Verify that the server correctly identifies the conflict within the workspace
- Ensure that the conflict information allows Client 2 to resolve the conflict locally
- Confirm that this conflict handling does not affect documents or users in other workspaces

### 7. Security Policy Testing

#### Test Case 7.1: Enforce Read Policy Across Workspaces

**Objective:** Verify that the read policy is correctly enforced across different workspaces and user roles.

**Preconditions:**
- RxDBDotNet server is running with security policies configured
- Users with different roles exist in different workspaces
- Documents exist in different workspaces

**Data Setup:**
- Create users with different roles in different workspaces:
  ```csharp
  var superAdmin = new User { Id = Guid.NewGuid(), FirstName = "Super", LastName = "Admin", Email = "superadmin@livedocs.com", Role = UserRole.SuperAdmin, WorkspaceId = liveDocsWorkspaceId, UpdatedAt = DateTimeOffset.UtcNow, IsDeleted = false };
  var workspaceAAdmin = new User { Id = Guid.NewGuid(), FirstName = "Admin", LastName = "A", Email = "admin@workspacea.com", Role = UserRole.Admin, WorkspaceId = workspaceAId, UpdatedAt = DateTimeOffset.UtcNow, IsDeleted = false };
  var workspaceBUser = new User { Id = Guid.NewGuid(), FirstName = "User", LastName = "B", Email = "user@workspaceb.com", Role = UserRole.User, WorkspaceId = workspaceBId, UpdatedAt = DateTimeOffset.UtcNow, IsDeleted = false };
  ```
- Create documents in different workspaces

**Execution Flow:**
1. Attempt to read documents as a super admin user
2. Attempt to read documents as a workspace admin user
3. Attempt to read documents as a regular user
4. Attempt to read documents from a different workspace for each user role

**Expected Results:**
- Super admin user should be able to read all documents across all workspaces
- Workspace admin user should be able to read all documents within their workspace but not from other workspaces
- Regular user should only be able to read documents within their workspace
- No user should be able to read documents from workspaces they don't belong to (except super admin)

#### Test Case 7.2: Enforce Write Policy Across Workspaces

**Objective:** Verify that the write policy is correctly enforced across different workspaces and user roles.

**Preconditions:**
- RxDBDotNet server is running with security policies configured
- Users with different roles exist in different workspaces
- Documents exist in different workspaces

**Data Setup:**
- Use the users and documents from Test Case 7.1

**Execution Flow:**
1. Attempt to create a new document in various workspaces as different user roles
2. Attempt to update existing documents in various workspaces as different user roles
3. Attempt to delete documents in various workspaces as different user roles

**Expected Results:**
- Super admin user should be able to create, update, and delete documents across all workspaces
- Workspace admin user should be able to create, update, and delete documents only within their workspace
- Regular user should be able to create new documents within their workspace, update their own documents, but not delete any documents
- No user should be able to modify documents in workspaces they don't belong to (except super admin)

#### Test Case 7.3: Workspace-based Subscription Filtering

**Objective:** Verify that subscriptions only emit updates for documents within the user's workspace.

**Preconditions:**
- RxDBDotNet server is running with security policies configured
- Users with different roles exist in different workspaces
- Documents exist in different workspaces

**Data Setup:**
- Use the users and documents from Test Case 7.1

**Execution Flow:**
1. Start subscriptions for document changes as different user roles in different workspaces
2. Perform various document operations (create, update, delete) across different workspaces

**Expected Results:**
- Super admin subscription should receive updates for document changes across all workspaces
- Workspace admin subscription should only receive updates for document changes within their workspace
- Regular user subscription should only receive updates for document changes within their workspace
- No user should receive updates for document changes in workspaces they don't belong to (except super admin)
