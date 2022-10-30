(window.webpackJsonp=window.webpackJsonp||[]).push([[190],{279:function(e,t,a){"use strict";a.r(t),a.d(t,"frontMatter",(function(){return l})),a.d(t,"rightToc",(function(){return i})),a.d(t,"metadata",(function(){return c})),a.d(t,"default",(function(){return d}));var n=a(2),r=a(6),b=(a(0),a(288)),l={id:"region",title:"Region",sidebar_label:"Region"},i=[{value:"Boundary",id:"boundary",children:[]},{value:"Normal",id:"normal",children:[]}],c={id:"contract-builder-api/nodes/region",title:"Region",description:"The `Region` node allows for creation of regions in the map. These are used for various purposes like trigger points, ensuring the player/AI stays within an area and using it for randomly selecting units/buildings within the region.\r",source:"@site/docs\\contract-builder-api\\nodes\\region.md",permalink:"/docs/contract-builder-api/nodes/region",sidebar_label:"Region",sidebar:"docs",previous:{title:"Objective",permalink:"/docs/contract-builder-api/nodes/objective"},next:{title:"Spawner",permalink:"/docs/contract-builder-api/nodes/spawner"}},o={rightToc:i,metadata:c};function d(e){var t=e.components,a=Object(r.a)(e,["components"]);return Object(b.b)("wrapper",Object(n.a)({},o,a,{components:t,mdxType:"MDXLayout"}),Object(b.b)("p",null,"The ",Object(b.b)("inlineCode",{parentName:"p"},"Region")," node allows for creation of regions in the map. These are used for various purposes like trigger points, ensuring the player/AI stays within an area and using it for randomly selecting units/buildings within the region."),Object(b.b)("h2",{id:"boundary"},"Boundary"),Object(b.b)("p",null,"This node defines the ",Object(b.b)("inlineCode",{parentName:"p"},"Encounter")," boundaries. This is effectively what the player will consider the 'map boundary', however, the real map boundary is always 2k by 2k in size. The ",Object(b.b)("inlineCode",{parentName:"p"},"Encounter")," boundary is the playable space within the map."),Object(b.b)("table",null,Object(b.b)("thead",{parentName:"table"},Object(b.b)("tr",{parentName:"thead"},Object(b.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Property"),Object(b.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Required"),Object(b.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Default"),Object(b.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Details"))),Object(b.b)("tbody",{parentName:"table"},Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Name"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"true"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"-"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Name of the Node that will be used for the Unity game object")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Type"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"true"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"Region")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Type of node")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"SubType"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"true"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"Boundary")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Subtype of node")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Position"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"false"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"-"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Position of the center of the boundary region.")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Width"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"false"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"800")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Width measure of the map (maximum of ",Object(b.b)("inlineCode",{parentName:"td"},"2000"),")")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Length"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"false"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"800")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Length measure of the map (maximum of ",Object(b.b)("inlineCode",{parentName:"td"},"2000"),")")))),Object(b.b)("h4",{id:"example"},"Example"),Object(b.b)("pre",null,Object(b.b)("code",Object(n.a)({parentName:"pre"},{className:"language-json"}),'{\n  "Name": "EncounterBoundaryRect",\n  "Type": "Region",\n  "SubType": "Boundary",\n  "Position": {\n    "Type": "World", // World, Local\n    "Value": { "x": 0, "y": 0, "z": 0 }\n  },\n  "Width": 1024,\n  "Length": 1024\n}\n')),Object(b.b)("h2",{id:"normal"},"Normal"),Object(b.b)("p",null,"This node defines the ",Object(b.b)("inlineCode",{parentName:"p"},"Encounter")," boundaries. This is effectively what the player will consider the 'map boundary', however, the real map boundary is always 2k by 2k in size. The ",Object(b.b)("inlineCode",{parentName:"p"},"Encounter")," boundary is the playable space within the map."),Object(b.b)("table",null,Object(b.b)("thead",{parentName:"table"},Object(b.b)("tr",{parentName:"thead"},Object(b.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Property"),Object(b.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Required"),Object(b.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Default"),Object(b.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Details"))),Object(b.b)("tbody",{parentName:"table"},Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Name"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"true"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"-"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Name of the Node that will be used for the Unity game object")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Type"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"true"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"Region")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Type of node")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"SubType"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"true"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"Normal")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Subtype of node")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Position"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"false"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"-"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Position of the center of the boundary region.")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Rotation"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"false"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"-"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Regions are hexigon in shape. Rotating them can affect the trigger area and may be useful under specific situations.")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Guid"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"false"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"-"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"A ",Object(b.b)("a",Object(n.a)({parentName:"td"},{href:"https://www.uuidgenerator.net/"}),"UUIDv4")," that you then use in the contract json.")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"ObjectiveGuid"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"false"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"-"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Guid of the associated objective (if there is one)")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Radius"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"false"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"0")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Radius of the hexigon region")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"RegionDefId"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"false"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"regionDef_TargetZone")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Affects the region colour and the message on hover, like 'Target'. ",Object(b.b)("br",null),Object(b.b)("br",null),"Usable: ",Object(b.b)("inlineCode",{parentName:"td"},"regionDef_Positive"),", ",Object(b.b)("inlineCode",{parentName:"td"},"regionDef_Negative"),", ",Object(b.b)("inlineCode",{parentName:"td"},"regionDef_HostileDropZone"),", ",Object(b.b)("inlineCode",{parentName:"td"},"regionDef_EvacZone"),", ",Object(b.b)("inlineCode",{parentName:"td"},"regionDef_DangerZone"),", ",Object(b.b)("inlineCode",{parentName:"td"},"regionDef_CaptureZone"),", ",Object(b.b)("inlineCode",{parentName:"td"},"regionDef_TargetZone"),", ",Object(b.b)("inlineCode",{parentName:"td"},"regionDef_EscortZone"),", ",Object(b.b)("inlineCode",{parentName:"td"},"regionDef_DenialZone"))))),Object(b.b)("h4",{id:"example-1"},"Example"),Object(b.b)("pre",null,Object(b.b)("code",Object(n.a)({parentName:"pre"},{className:"language-json"}),'{\n  "Name": "Region_Investigate_Blackout",\n  "Type": "Region",\n  "SubType": "Normal",\n  "Guid": "e7e9f35b-7ed8-404e-9dae-69be61de2dd3", // Must match the region guid in the contract .json\n  "ObjectiveGuid": "786166e2-22ea-45c1-9786-68df31958bd8", // Must match the objective guid in the build file to link to\n  "RegionDefId": "regionDef_TargetZone",\n  "Position": {\n    // Usually used in the map override file\n    "Type": "World", // World, Local\n    "Value": { "x": -320, "y": 0, "z": 260 }\n  },\n  "Rotation": {\n    // Usually used in the map override file\n    "Type": "World", // World, Local\n    "Value": { "x": 0, "y": 0, "z": 0 }\n  },\n  "Radius": 160\n}\n')))}d.isMDXComponent=!0},288:function(e,t,a){"use strict";a.d(t,"a",(function(){return i})),a.d(t,"b",(function(){return d}));var n=a(0),r=a.n(n),b=r.a.createContext({}),l=function(e){var t=r.a.useContext(b),a=t;return e&&(a="function"==typeof e?e(t):Object.assign({},t,e)),a},i=function(e){var t=l(e.components);return r.a.createElement(b.Provider,{value:t},e.children)};var c={inlineCode:"code",wrapper:function(e){var t=e.children;return r.a.createElement(r.a.Fragment,{},t)}},o=Object(n.forwardRef)((function(e,t){var a=e.components,n=e.mdxType,b=e.originalType,i=e.parentName,o=function(e,t){var a={};for(var n in e)Object.prototype.hasOwnProperty.call(e,n)&&-1===t.indexOf(n)&&(a[n]=e[n]);return a}(e,["components","mdxType","originalType","parentName"]),d=l(a),p=n,j=d[i+"."+p]||d[p]||c[p]||b;return a?r.a.createElement(j,Object.assign({},{ref:t},o,{components:a})):r.a.createElement(j,Object.assign({},{ref:t},o))}));function d(e,t){var a=arguments,n=t&&t.mdxType;if("string"==typeof e||n){var b=a.length,l=new Array(b);l[0]=o;var i={};for(var c in t)hasOwnProperty.call(t,c)&&(i[c]=t[c]);i.originalType=e,i.mdxType="string"==typeof e?e:n,l[1]=i;for(var d=2;d<b;d++)l[d]=a[d];return r.a.createElement.apply(null,l)}return r.a.createElement.apply(null,a)}o.displayName="MDXCreateElement"}}]);