/**
 * This file is derived from the RxDB project (https://github.com/pubkey/rxdb).
 *
 * This file is licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * Original Source: https://github.com/pubkey/rxdb
 */


export const GRAPHQL_PORT = 10102;
export const GRAPHQL_PATH = "/graphql";
export const GRAPHQL_SUBSCRIPTION_PORT = 10103;
export const GRAPHQL_SUBSCRIPTION_PATH = "/subscriptions";

/**
 * to demonstrate how auth would work,
 * we use these jwt tokens
 */
export const JWT_PRIVATE_KEY = "qwertyuiopasdfghjklzxcvbnm123456"; // H256
export const JWT_BEARER_TOKEN = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJyeGRiIiwiaWF0IjoxNjA0MTg4Njk3LCJ" +
    "leHAiOjIyNjY4NzY2OTcsImF1ZCI6InJ4ZGIuaW5mbyIsInN1YiI6InVzZXJAcnhkYi5pbmZvIn0.hNEC3V4LpkEvGnLeT8hNTXTCZRPpMVDwaltH-8zh4Iw";

export const heroSchema = {
    version: 0,
    primaryKey: "id",
    type: "object",
    properties: {
        id: {
            type: "string",
            maxLength: 100
        },
        name: {
            type: "string",
            maxLength: 100
        },
        color: {
            type: "string",
            maxLength: 30
        },
        updatedAt: {
            type: "number",
            minimum: 0,
            maximum: 1000000000000000,
            multipleOf: 1
        }
    },
    indexes: ["name", "color", "updatedAt"],
    required: ["id", "color", "updatedAt"]
};

export const graphQLGenerationInput = {
    hero: {
        schema: heroSchema,
        checkpointFields: [
            "id",
            "updatedAt"
        ],
        deletedField: "deleted",
        headerFields: ["Authorization"]
    }
};
