(window.webpackJsonp=window.webpackJsonp||[]).push([[79],{169:function(e,n,t){"use strict";t.r(n),t.d(n,"frontMatter",(function(){return c})),t.d(n,"rightToc",(function(){return i})),t.d(n,"metadata",(function(){return u})),t.d(n,"default",(function(){return l}));var a=t(2),o=t(6),r=(t(0),t(260)),c={id:"custom-contract-types",title:"Custom Contract Types"},i=[{value:"Contract Type Build File",id:"contract-type-build-file",children:[]}],u={id:"features/custom-contract-types",title:"Custom Contract Types",description:"Mission Control adds and builds custom contract types into the game. It does this using a builder approach. A json contract type build file defines how the encounter layer should be and behave, then a normal contract .json file interacts with it like normal.\r",source:"@site/docs\\features\\custom-contract-types.md",permalink:"/docs/features/custom-contract-types",sidebar:"docs",previous:{title:"Settings",permalink:"/docs/features/settings"},next:{title:"Reuse of Story Maps",permalink:"/docs/features/reuse-of-story-maps"}},s={rightToc:i,metadata:u};function l(e){var n=e.components,t=Object(o.a)(e,["components"]);return Object(r.b)("wrapper",Object(a.a)({},s,t,{components:n,mdxType:"MDXLayout"}),Object(r.b)("p",null,"Mission Control adds and builds custom contract types into the game. It does this using a builder approach. A json contract type build file defines how the encounter layer should be and behave, then a normal contract .json file interacts with it like normal."),Object(r.b)("p",null,"Custom contract types can even be used to create new story contracts, or make much more unique flashpoint missions. It's still a heavy work in progress but more features will be added to it to support great contract creation."),Object(r.b)("h2",{id:"contract-type-build-file"},"Contract Type Build File"),Object(r.b)("p",null,"The contract type build files exist in the ",Object(r.b)("inlineCode",{parentName:"p"},"MissionControl/contractTypeBuilds/")," folder. Each file defines how the custom contract type should be built."),Object(r.b)("pre",null,Object(r.b)("code",Object(a.a)({parentName:"pre"},{className:"language-json"}),'{\n  "Key": "SoloDuel", // Links to the ContractType Name\n  "Chunks": [\n    {\n      "Name": "Chunk_PlayerLance",\n      "Type": "Chunk",\n      "SubType": "PlayerLance",\n      "Children": [\n        {\n          "Name": "Spawner_PlayerLance",\n          "Type": "Spawner",\n          "SubType": "SimpleSpawner",\n          "Position": {\n            "Type": "World", // World, Local\n            "Value": { "x": -90, "y": 86, "z": 40 }\n          },\n          "Rotation": {\n            "Type": "World", // World, Local\n            "Value": { "x": 0, "y": 30, "z": 0 }\n          },\n          "Team": "Player1",\n          "Guid": "76b654a6-4f2c-4a6f-86e6-d4cf868335fe", // Must be this Guid and the contract .json must have this specific. It\'s hardcoded in BT for PlayerLance.\n          "SpawnPoints": 1,\n          "SpawnPointGuids": ["ec9d2280-ca9a-4d90-8a20-963d8a4c0a39"], // Must match the unit spawn guids in the contract .json\n          "SpawnType": "Instant" // Leopard, DropPod, Instant\n        }\n      ]\n    },\n    {\n      "Name": "Chunk_DestroyWholeLance",\n      "Type": "Chunk",\n      "SubType": "DestroyWholeLance",\n      "Position": {\n        "Type": "World", // World, Local\n        "Value": { "x": 0, "y": 0, "z": 0 }\n      },\n      "Children": [\n        {\n          "Name": "Lance_Enemy_OpposingForce",\n          "Type": "Spawner",\n          "SubType": "SimpleSpawner",\n          "Position": {\n            "Type": "World", // World, Local\n            "Value": { "x": 0, "y": 60, "z": 650 }\n          },\n          "Rotation": {\n            "Type": "World", // World, Local\n            "Value": { "x": 0, "y": 180, "z": 0 }\n          },\n          "Team": "Target",\n          "Guid": "f426f0dc-969d-477d-81a9-d02f9e1eff79", // Must match the spawner guids in the contract .json\n          "SpawnPoints": 1,\n          "SpawnPointGuids": ["6cd3107e-0f9d-4809-ab8c-fb30faf4cd80"], // Must match the unit spawn guids in the contract .json\n          "SpawnType": "Instant" // Leopard, DropPod, Instant\n        },\n        {\n          "Name": "Objective_DestroyLance",\n          "Type": "Objective",\n          "SubType": "DestroyLance",\n          "Guid": "a0b9c5b2-c594-4c5a-be1d-028a51c51519", // Must match the objective guid in the contract .json\n          "ContractObjectiveGuid": "73275787-720a-4c33-9f20-953b1bbf48bd", // Must match the contract guid in the contract .json\n          "Title": "Destroy the enemy lance",\n          "Priority": 1,\n          "IsPrimaryObjective": true,\n          "LanceToDestroyGuid": "f426f0dc-969d-477d-81a9-d02f9e1eff79"\n        }\n      ]\n    },\n    {\n      "Name": "Chunk_EncounterBoundary",\n      "Type": "Chunk",\n      "SubType": "EncounterBoundary",\n      "Children": [\n        {\n          "Name": "EncounterBoundaryRect",\n          "Type": "Region",\n          "SubType": "Boundary",\n          "Position": {\n            "Type": "World", // World, Local\n            "Value": { "x": 15, "y": 50, "z": 450 }\n          },\n          "Width": 900,\n          "Length": 1024\n        }\n      ]\n    },\n    {\n      "Name": "Chunk_DefaultDialogue",\n      "Type": "Chunk",\n      "SubType": "Dialogue",\n      "Children": [\n        {\n          "Name": "Dialogue_MissionStart",\n          "Type": "Dialogue",\n          "SubType": "Simple",\n          "Guid": "73df8d9c-a274-48fd-98c9-2bd0d7860e83", // Must match the dialogue guid in the contract .json\n          "ShowOnlyOnce": true\n        },\n        {\n          "Name": "Dialogue_MissionSuccess",\n          "Type": "Dialogue",\n          "SubType": "Simple",\n          "Guid": "4011a4c3-cba2-4d22-b2b3-3b19a3297ab9", // Must match the dialogue guid in the contract .json\n          "ShowOnlyOnce": true\n        },\n        {\n          "Name": "Dialogue_MissionFailure",\n          "Type": "Dialogue",\n          "SubType": "Simple",\n          "Guid": "d3d33d95-9ed7-4686-b9eb-954ebe51cc02", // Must match the dialogue guid in the contract .json\n          "ShowOnlyOnce": true\n        }\n      ]\n    },\n    {\n      "Name": "Chunk_DuelTaunt",\n      "Type": "Chunk",\n      "SubType": "Dialogue",\n      "Children": [\n        {\n          "Name": "Dialogue_DuelTaunt",\n          "Type": "Dialogue",\n          "SubType": "Simple",\n          "Guid": "8971ddc6-a882-4066-923f-f8be03450ce2", // Must match the dialogue guid in the contract .json\n          "Trigger": "OnFirstContact"\n        }\n      ]\n    },\n    {\n      "Name": "Chunk_SwapSpawnerPlacement",\n      "Type": "Chunk",\n      "SubType": "Placement",\n      "ControlledByContract": true,\n      "Guid": "ed007c52-f4cb-4bfc-842a-a50454d8a82a",\n      "Children": [\n        {\n          "Name": "SwapPlacement_SwapLanceSpawners",\n          "Type": "SwapPlacement",\n          "SubType": "EncounterStructure",\n          "TargetGuid1": "76b654a6-4f2c-4a6f-86e6-d4cf868335fe", // Player spawner\n          "TargetGuid2": "f426f0dc-969d-477d-81a9-d02f9e1eff79" // Enemy spawner\n        }\n      ]\n    }\n  ]\n}\n')),Object(r.b)("p",null,Object(r.b)("em",{parentName:"p"},Object(r.b)("strong",{parentName:"em"},"A full breakdown explaining the above will be added soon"))))}l.isMDXComponent=!0},260:function(e,n,t){"use strict";t.d(n,"a",(function(){return i})),t.d(n,"b",(function(){return l}));var a=t(0),o=t.n(a),r=o.a.createContext({}),c=function(e){var n=o.a.useContext(r),t=n;return e&&(t="function"==typeof e?e(n):Object.assign({},n,e)),t},i=function(e){var n=c(e.components);return o.a.createElement(r.Provider,{value:n},e.children)};var u={inlineCode:"code",wrapper:function(e){var n=e.children;return o.a.createElement(o.a.Fragment,{},n)}},s=Object(a.forwardRef)((function(e,n){var t=e.components,a=e.mdxType,r=e.originalType,i=e.parentName,s=function(e,n){var t={};for(var a in e)Object.prototype.hasOwnProperty.call(e,a)&&-1===n.indexOf(a)&&(t[a]=e[a]);return t}(e,["components","mdxType","originalType","parentName"]),l=c(t),d=a,p=l[i+"."+d]||l[d]||u[d]||r;return t?o.a.createElement(p,Object.assign({},{ref:n},s,{components:t})):o.a.createElement(p,Object.assign({},{ref:n},s))}));function l(e,n){var t=arguments,a=n&&n.mdxType;if("string"==typeof e||a){var r=t.length,c=new Array(r);c[0]=s;var i={};for(var u in n)hasOwnProperty.call(n,u)&&(i[u]=n[u]);i.originalType=e,i.mdxType="string"==typeof e?e:a,c[1]=i;for(var l=2;l<r;l++)c[l]=t[l];return o.a.createElement.apply(null,c)}return o.a.createElement.apply(null,t)}s.displayName="MDXCreateElement"}}]);