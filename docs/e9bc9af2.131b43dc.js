(window.webpackJsonp=window.webpackJsonp||[]).push([[153],{241:function(e,t,a){"use strict";a.r(t),a.d(t,"frontMatter",(function(){return b})),a.d(t,"rightToc",(function(){return l})),a.d(t,"metadata",(function(){return i})),a.d(t,"default",(function(){return u}));var n=a(2),r=a(6),c=(a(0),a(260)),b={id:"chunks",title:"Chunks",sidebar_label:"Chunks"},l=[{value:"Chunk Structure",id:"chunk-structure",children:[]},{value:"Chunk SubTypes",id:"chunk-subtypes",children:[]}],i={id:"contract-builder-api/chunks",title:"Chunks",description:"A chunk is a collection of game logic game objects which are related. A named chunk, e.g. `PlayerLance`, often has special logic associated with it whilst using chunks purely as a logical collection of 'like' logics can also be used, e.g. `Container`.\r",source:"@site/docs\\contract-builder-api\\chunks.md",permalink:"/docs/contract-builder-api/chunks",sidebar_label:"Chunks",sidebar:"docs",previous:{title:"Understanding Structure",permalink:"/docs/contract-builder/structure"},next:{title:"Nodes",permalink:"/docs/contract-builder-api/nodes"}},o={rightToc:l,metadata:i};function u(e){var t=e.components,a=Object(r.a)(e,["components"]);return Object(c.b)("wrapper",Object(n.a)({},o,a,{components:t,mdxType:"MDXLayout"}),Object(c.b)("p",null,"A chunk is a collection of game logic game objects which are related. A named chunk, e.g. ",Object(c.b)("inlineCode",{parentName:"p"},"PlayerLance"),", often has special logic associated with it whilst using chunks purely as a logical collection of 'like' logics can also be used, e.g. ",Object(c.b)("inlineCode",{parentName:"p"},"Container"),"."),Object(c.b)("p",null,"Under a chunk you create ",Object(c.b)("inlineCode",{parentName:"p"},"Node")," children. A node is a specific logic piece like the ability to place a ",Object(c.b)("inlineCode",{parentName:"p"},"Spawner")," or create an ",Object(c.b)("inlineCode",{parentName:"p"},"Objective"),"."),Object(c.b)("h3",{id:"chunk-structure"},"Chunk Structure"),Object(c.b)("pre",null,Object(c.b)("code",Object(n.a)({parentName:"pre"},{className:"language-json"}),'{\n  "Name": "Chunk_Optional_Initial_Enemy",\n  "Type": "Chunk",\n  "SubType": "Lance",\n  "StartingStatus": "Inactive", // Optional\n  "ConflictsWith": ["9c65494e-4e1a-1234-9b47-666ab8fc1111"], // Optional\n  "OnActiveExecute": [\n    // Optional\n    {\n      "Type": "Dialogue",\n      "EncounterGuid": "e0ca3227-ffbf-4088-a261-3d4e9ab7d4c5"\n    },\n    {\n      "Type": "SetChunkStateAtRandom",\n      "ChunkGuids": ["c27a41c7-ae4d-4a97-90be-3e710fe31e22"]\n    }\n  ],\n  "ControlledByContract": true, // Optional\n  "Guid": "3b47894e-2d25-4599-9b47-620ab8fcfa62", // Optional\n  "Children": [\n    // Children nodes\n  ]\n}\n')),Object(c.b)("table",null,Object(c.b)("thead",{parentName:"table"},Object(c.b)("tr",{parentName:"thead"},Object(c.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Property"),Object(c.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Required"),Object(c.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Default"),Object(c.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Details"))),Object(c.b)("tbody",{parentName:"table"},Object(c.b)("tr",{parentName:"tbody"},Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Name"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"true"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"-"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Name of the chunk that will be used for the Unity game object")),Object(c.b)("tr",{parentName:"tbody"},Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Type"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"true"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"-"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Type of node")),Object(c.b)("tr",{parentName:"tbody"},Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"SubType"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"true"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"-"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Subtype of chunk")),Object(c.b)("tr",{parentName:"tbody"},Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"StartingStatus"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"false"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(c.b)("inlineCode",{parentName:"td"},"Active")),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Determines the starting status. ",Object(c.b)("inlineCode",{parentName:"td"},"Active"),", ",Object(c.b)("inlineCode",{parentName:"td"},"Inactive"),", ",Object(c.b)("inlineCode",{parentName:"td"},"Finished"))),Object(c.b)("tr",{parentName:"tbody"},Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"ConflictsWith"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"false"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(c.b)("inlineCode",{parentName:"td"},"[]")),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Sets which chunks would conflict with this one. Important for ensuring no loose ending conditions. ",Object(c.b)("br",null),Object(c.b)("br",null)," This marks any objective in the conflicting chunk as non-primary to allow complex contracts not to be blocked with locked contract objectives")),Object(c.b)("tr",{parentName:"tbody"},Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"OnActiveExecute"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"false"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(c.b)("inlineCode",{parentName:"td"},"[]")),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Sets which logic to execute when this chunk is changed from ",Object(c.b)("inlineCode",{parentName:"td"},"Inactive")," to ",Object(c.b)("inlineCode",{parentName:"td"},"Active"),".",Object(c.b)("br",null),Object(c.b)("br",null),"Currently supported logic is: ",Object(c.b)("inlineCode",{parentName:"td"},"Dialogue")," and ",Object(c.b)("inlineCode",{parentName:"td"},"SetChunkStateAtRandom")," as seen in the above example")),Object(c.b)("tr",{parentName:"tbody"},Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"ControlledByContract"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"false"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(c.b)("inlineCode",{parentName:"td"},"false")),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Exposes the chunk to be enabled/disabled in the contract .json under 'chunkList'")),Object(c.b)("tr",{parentName:"tbody"},Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Guid"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"false"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"-"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Can be used to manually specify a Guid for use by other chunks, triggers, results or conditions")),Object(c.b)("tr",{parentName:"tbody"},Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Children"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"false"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(c.b)("inlineCode",{parentName:"td"},"[]")),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"An array of ",Object(c.b)("a",Object(n.a)({parentName:"td"},{href:"nodes"}),"Nodes")," that specify logic to execute")))),Object(c.b)("h3",{id:"chunk-subtypes"},"Chunk SubTypes"),Object(c.b)("table",null,Object(c.b)("thead",{parentName:"table"},Object(c.b)("tr",{parentName:"thead"},Object(c.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Type"),Object(c.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Details"))),Object(c.b)("tbody",{parentName:"table"},Object(c.b)("tr",{parentName:"tbody"},Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Container"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"General use chunk to group related logic together. Can be used for almost anything")),Object(c.b)("tr",{parentName:"tbody"},Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"PlayerLance"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Used to organise player lance spawning")),Object(c.b)("tr",{parentName:"tbody"},Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"DestroyWholeLance"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Used to organise a 'Destroy Lance' setup")),Object(c.b)("tr",{parentName:"tbody"},Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"EncounterBoundary"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Used to organise the encounter boundary setup")),Object(c.b)("tr",{parentName:"tbody"},Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Dialogue"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Used to organise dialogue")),Object(c.b)("tr",{parentName:"tbody"},Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Placement"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Used to organise placement related logic")),Object(c.b)("tr",{parentName:"tbody"},Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Lance"),Object(c.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Used to organise spawning of a lance")))))}u.isMDXComponent=!0},260:function(e,t,a){"use strict";a.d(t,"a",(function(){return l})),a.d(t,"b",(function(){return u}));var n=a(0),r=a.n(n),c=r.a.createContext({}),b=function(e){var t=r.a.useContext(c),a=t;return e&&(a="function"==typeof e?e(t):Object.assign({},t,e)),a},l=function(e){var t=b(e.components);return r.a.createElement(c.Provider,{value:t},e.children)};var i={inlineCode:"code",wrapper:function(e){var t=e.children;return r.a.createElement(r.a.Fragment,{},t)}},o=Object(n.forwardRef)((function(e,t){var a=e.components,n=e.mdxType,c=e.originalType,l=e.parentName,o=function(e,t){var a={};for(var n in e)Object.prototype.hasOwnProperty.call(e,n)&&-1===t.indexOf(n)&&(a[n]=e[n]);return a}(e,["components","mdxType","originalType","parentName"]),u=b(a),d=n,p=u[l+"."+d]||u[d]||i[d]||c;return a?r.a.createElement(p,Object.assign({},{ref:t},o,{components:a})):r.a.createElement(p,Object.assign({},{ref:t},o))}));function u(e,t){var a=arguments,n=t&&t.mdxType;if("string"==typeof e||n){var c=a.length,b=new Array(c);b[0]=o;var l={};for(var i in t)hasOwnProperty.call(t,i)&&(l[i]=t[i]);l.originalType=e,l.mdxType="string"==typeof e?e:n,b[1]=l;for(var u=2;u<c;u++)b[u]=a[u];return r.a.createElement.apply(null,b)}return r.a.createElement.apply(null,a)}o.displayName="MDXCreateElement"}}]);