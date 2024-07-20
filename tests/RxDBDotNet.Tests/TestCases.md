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

## Test Data Model

```csharp
/// <summary>
/// Represents a live, collaborative document within the LiveDocs system.
/// </summary>
/// <remarks>
/// <para>
/// A LiveDoc is the core entity in the LiveDocs collaborative editing platform. It encapsulates
/// the content and metadata of a single document that can be collaboratively edited in real-time
/// by multiple users. This class implements the IReplicatedDocument interface, enabling it to be
/// efficiently synchronized across multiple clients and the server using the RxDB replication protocol.
/// </para>
/// <para>
/// Each LiveDoc has a unique identifier, content that can be edited, an owner, and timestamps for
/// tracking updates. The IsDeleted property supports soft deletion, allowing for document recovery
/// and maintaining a consistent history of changes.
/// </para>
/// <para>
/// As an IReplicatedDocument, LiveDoc instances are automatically handled by the RxDBDotNet
/// replication system, ensuring real-time updates, conflict resolution, and offline support
/// across all connected clients.
/// </para>
/// </remarks>
/// <seealso cref="IReplicatedDocument"/>
public class LiveDoc : IReplicatedDocument
{
    /// <summary>
    /// Gets or initializes the unique identifier for the live doc.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the content of the live doc.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Gets or initializes the unique identifier of the live doc's owner.
    /// </summary>
    public required Guid OwnerId { get; init; }

    /// <summary>
    /// Gets or sets the date and time when the live doc was last updated.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the live doc has been deleted.
    /// </summary>
    public required bool IsDeleted { get; set; }
}

/// <summary>
/// Represents a user of the LiveDocs system.
/// </summary>
public class User : IReplicatedDocument
{
    /// <summary>
    /// Gets or initializes the unique identifier for the user.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the username of the user.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// Gets or sets the role of the user.
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user account was last updated.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user account has been deleted.
    /// </summary>
    public required bool IsDeleted { get; set; }
}
```

## Test Cases

### 1. User Management

#### Test Case 1.1: Create a new user

**Objective:** Verify that a new user can be created in the system.

**Preconditions:**
- RxDBDotNet server is running and accessible
- User collection is empty

**Data Setup:**
- New user data:
  ```csharp
  var newUser = new TestUser
  {
      Id = Guid.NewGuid(),
      Username = "testuser1",
      Role = "User",
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```

**Execution Flow:**
1. Send a GraphQL mutation to create the new user
2. Retrieve the created user using a GraphQL query

**Expected Results:**
- The mutation should return a success response
- The query should return a user object matching the input data
- The user should be present in the database

**Additional Notes:**
- Verify that the UpdatedAt field is set to the current time
- Ensure that the IsDeleted field is set to false

#### Test Case 1.2: Retrieve an existing user

**Objective:** Verify that an existing user can be retrieved from the system.

**Preconditions:**
- RxDBDotNet server is running and accessible
- At least one user exists in the system (created in Test Case 1.1)

**Data Setup:**
- Use the user created in Test Case 1.1

**Execution Flow:**
1. Send a GraphQL query to retrieve the user by ID

**Expected Results:**
- The query should return the user object matching the given ID
- All fields should match the data from Test Case 1.1

### 2. Document Operations

#### Test Case 2.1: Create a new document

**Objective:** Verify that a new document can be created in the system.

**Preconditions:**
- RxDBDotNet server is running and accessible
- At least one user exists in the system
- Document collection is empty

**Data Setup:**
- Existing user ID: [Use the ID of the user created in Test Case 1.1]
- New document data:
  ```csharp
  var newDocument = new TestDocument
  {
      Id = Guid.NewGuid(),
      Content = "This is a test document.",
      OwnerId = [Existing user ID],
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```

**Execution Flow:**
1. Send a GraphQL mutation to create the new document
2. Retrieve the created document using a GraphQL query

**Expected Results:**
- The mutation should return a success response
- The query should return a document object matching the input data
- The document should be present in the database

**Additional Notes:**
- Verify that the UpdatedAt field is set to the current time
- Ensure that the IsDeleted field is set to false
- Check that the OwnerId matches the ID of the user who created the document

### 3. Pull Operations

#### Test Case 3.1: Initial Pull (Checkpoint Iteration)

**Objective:** Verify the correct implementation of the initial pull operation with a null checkpoint.

**Preconditions:**
- RxDBDotNet server is running and accessible
- Multiple documents exist in the system

**Data Setup:**
- Create at least 3 documents with known content and timestamps:
  ```csharp
  var documents = new List<TestDocument>
  {
      new TestDocument
      {
          Id = Guid.NewGuid(),
          Content = "First document",
          OwnerId = [Existing user ID],
          UpdatedAt = DateTimeOffset.UtcNow.AddHours(-2),
          IsDeleted = false
      },
      new TestDocument
      {
          Id = Guid.NewGuid(),
          Content = "Second document",
          OwnerId = [Existing user ID],
          UpdatedAt = DateTimeOffset.UtcNow.AddHours(-1),
          IsDeleted = false
      },
      new TestDocument
      {
          Id = Guid.NewGuid(),
          Content = "Third document",
          OwnerId = [Existing user ID],
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

**Additional Notes:**
- Verify that the documents are returned in the correct order (oldest to newest)
- Ensure that the checkpoint in the response matches the UpdatedAt and ID of the newest document

#### Test Case 3.2: Subsequent Pull

**Objective:** Ensure proper handling of pulls with a valid checkpoint.

**Preconditions:**
- RxDBDotNet server is running and accessible
- Multiple documents exist in the system, including some created after the checkpoint

**Data Setup:**
- Use the documents from Test Case 3.1
- Create a new document after recording the checkpoint from the previous pull:
  ```csharp
  var newDocument = new TestDocument
  {
      Id = Guid.NewGuid(),
      Content = "Fourth document",
      OwnerId = [Existing user ID],
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```

**Execution Flow:**
1. Send a GraphQL query to pull documents with the checkpoint from Test Case 3.1 and a limit of 10

**Expected Results:**
- The query should return only the documents created or updated after the given checkpoint
- In this case, it should return only the "Fourth document"
- The response should include a new checkpoint based on the latest document

**Additional Notes:**
- Verify that documents created before or at the checkpoint time are not included in the response
- Ensure that the new checkpoint in the response matches the UpdatedAt and ID of the newest document

#### Test Case 3.3: Checkpoint iteration to pull a large number of documents in small "batches"

**Objective:** Verify correct implementation of checkpoint iteration operations using checkpoints and limits.

**Preconditions:**
- RxDBDotNet server is running and accessible
- A large number of documents exist in the system (e.g., 250)

**Data Setup:**
- Create 250 documents with incremental UpdatedAt timestamps

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

### 4. Push Operations

#### Test Case 4.1: Push a new document

**Objective:** Verify correct handling of pushing a new document to the server.

**Preconditions:**
- RxDBDotNet server is running and accessible
- A user exists in the system

**Data Setup:**
- Existing user ID: [Use the ID of a previously created user]
- New document data:
  ```csharp
  var newDocument = new TestDocument
  {
      Id = Guid.NewGuid(),
      Content = "This document was pushed from the client",
      OwnerId = [Existing user ID],
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```

**Execution Flow:**
1. Send a GraphQL mutation to push the new document
2. Retrieve the pushed document using a GraphQL query

**Expected Results:**
- The mutation should return a success response
- The query should return a document matching the pushed data
- The document should be present in the database with all fields matching the pushed data

**Additional Notes:**
- Verify that the server doesn't modify any fields of the pushed document
- Ensure that a subsequent pull operation would include this new document

#### Test Case 4.2: Push an updated document

**Objective:** Ensure proper updating of existing documents through push operations.

**Preconditions:**
- RxDBDotNet server is running and accessible
- A document exists in the system

**Data Setup:**
- Existing document ID: [Use the ID of a previously created document]
- Updated document data:
  ```csharp
  var updatedDocument = new TestDocument
  {
      Id = [Existing document ID],
      Content = "This document was updated from the client",
      OwnerId = [Existing user ID],
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```

**Execution Flow:**
1. Retrieve the current state of the document
2. Send a GraphQL mutation to push the updated document, including the current server state as the assumedMasterState
3. Retrieve the updated document using a GraphQL query

**Expected Results:**
- The mutation should return a success response
- The query should return a document with the updated fields
- The document in the database should reflect the changes

**Additional Notes:**
- Verify that unchanged fields remain the same
- Ensure that a subsequent pull operation would include this updated document

### 5. Conflict Handling

#### Test Case 5.1: Detect and Report Conflict

**Objective:** Verify that conflicts are correctly detected and reported when pushing changes.

**Preconditions:**
- RxDBDotNet server is running and accessible
- A document exists in the system

**Data Setup:**
- Existing document ID: [Use the ID of a previously created document]
- Two client updates:
  ```csharp
  var clientUpdate1 = new TestDocument
  {
      Id = [Existing document ID],
      Content = "Update from Client 1",
      OwnerId = [Existing user ID],
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };

  var clientUpdate2 = new TestDocument
  {
      Id = [Existing document ID],
      Content = "Update from Client 2",
      OwnerId = [Existing user ID],
      UpdatedAt = DateTimeOffset.UtcNow,
      IsDeleted = false
  };
  ```

**Execution Flow:**
1. Both clients retrieve the current state of the document
2. Client 1 sends a push mutation with its update
3. Immediately after, Client 2 sends a push mutation with its update (using the original server state as assumedMasterState)
4. Both clients attempt to pull the latest document state

**Expected Results:**
- Client 1's push should succeed
- Client 2's push should be rejected due to conflict
- The pull operation should return the document with Client 1's update
- Client 2 should receive conflict information

**Additional Notes:**
- Verify that the server correctly identifies the conflict
- Ensure that the conflict information allows Client 2 to resolve the conflict locally

### 6. Real-time Updates (Subscriptions)

#### Test Case 6.1: Subscribe to Changes

**Objective:** Verify that subscriptions correctly emit real-time updates for document changes.

**Preconditions:**
- RxDBDotNet server is running and accessible
- GraphQL subscriptions are properly configured

**Execution Flow:**
1. Start a GraphQL subscription for document changes
2. Create a new document
3. Update an existing document
4. Delete (soft delete) an existing document

**Expected Results:**
- The subscription should receive a notification for each change (creation, update, deletion)
- Each notification should include the changed document and a new checkpoint

**Additional Notes:**
- Verify that the subscription receives changes in real-time
- Ensure that the checkpoint in each notification is updated correctly

### 7. Security Policy Testing

#### Test Case 7.1: Enforce Read Policy

**Objective:** Verify that the read policy is correctly enforced for different user roles.

**Preconditions:**
- RxDBDotNet server is running with security policies configured
- Users with different roles exist in the system
- Documents with different owners exist in the system

**Data Setup:**
- Create users with different roles:
  ```csharp
  var adminUser = new TestUser { Id = Guid.NewGuid(), Username = "admin", Role = "Admin", ... };
  var regularUser = new TestUser { Id = Guid.NewGuid(), Username = "user", Role = "User", ... };
  ```
- Create documents owned by different users

**Execution Flow:**
1. Attempt to read documents as an admin user
2. Attempt to read documents as a regular user
3. Attempt to read documents as an unauthenticated user

**Expected Results:**
- Admin user should be able to read all documents
- Regular user should only be able to read their own documents
- Unauthenticated user should not be able to read any documents

#### Test Case 7.2: Enforce Write Policy

**Objective:** Verify that the write policy is correctly enforced for different user roles.

**Preconditions:**
- RxDBDotNet server is running with security policies configured
- Users with different roles exist in the system
- Documents with different owners exist in the system

**Data Setup:**
- Use the users and documents from Test Case 7.1

**Execution Flow:**
1. Attempt to create a new document as different user roles
2. Attempt to update an existing document as different user roles
3. Attempt to delete a document as different user roles

**Expected Results:**
- Admin user should be able to create, update, and delete any document
- Regular user should only be able to create new documents and update/delete their own documents
- Unauthenticated user should not be able to perform any write operations

#### Test Case 7.3: Policy-based Subscription Filtering

**Objective:** Verify that subscriptions only emit updates for documents the user has permission to access.

**Preconditions:**
- RxDBDotNet server is running with security policies configured
- Users with different roles exist in the system
- Documents with different owners exist in the system

**Data Setup:**
- Use the users and documents from Test Case 7.1

**Execution Flow:**
1. Start subscriptions for document changes as different user roles
2. Perform various document operations (create, update, delete) on documents owned by different users

**Expected Results:**
- Admin subscription should receive updates for all document changes
- Regular user subscription should only receive updates for their own documents
- Unauthenticated user subscription should not receive any updates

These test cases provide a comprehensive coverage of RxDBDotNet's functionality, including the core replication features and security policy enforcement. They are ordered logically, building upon each other to create a complete test scenario. By implementing these test cases, you should achieve high code coverage of the RxDBDotNet library while also validating the security features you plan to implement.