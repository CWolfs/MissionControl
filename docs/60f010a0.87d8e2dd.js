(window.webpackJsonp=window.webpackJsonp||[]).push([[23],{113:function(e,t,a){"use strict";a.r(t),a.d(t,"frontMatter",(function(){return l})),a.d(t,"rightToc",(function(){return c})),a.d(t,"metadata",(function(){return i})),a.d(t,"default",(function(){return p}));var n=a(1),r=a(6),b=(a(0),a(157)),l={id:"extended-boundaries",title:"Extended Boundaries"},c=[{value:"Settings Breakdown",id:"settings-breakdown",children:[{value:"Overrides",id:"overrides",children:[]}]}],i={id:"features/extended-boundaries",title:"Extended Boundaries",description:"Increase the size of the encounter to the maximum available map size. This can sometimes be as much as around four times the size!",source:"@site/docs\\features\\extended-boundaries.md",permalink:"/docs/features/extended-boundaries",sidebar:"docs",previous:{title:"Extended Lances",permalink:"/docs/features/extended-lances"},next:{title:"Dynamic Withdraw",permalink:"/docs/features/dynamic-withdraw"}},d={rightToc:c,metadata:i},o="wrapper";function p(e){var t=e.components,a=Object(r.a)(e,["components"]);return Object(b.b)(o,Object(n.a)({},d,a,{components:t,mdxType:"MDXLayout"}),Object(b.b)("p",null,"Increase the size of the encounter to the maximum available map size. This can sometimes be as much as around four times the size!"),Object(b.b)("p",null,"Extended boundaries can increase the size of the contract type / encounter boundary (playable area in the map). In vanilla BT, contract types never use up to the maximum playable space (2k by 2k map size). With this feature, you can expand the boundary to either the maximum size (set as ",Object(b.b)("inlineCode",{parentName:"p"},"1")," for the ",Object(b.b)("inlineCode",{parentName:"p"},"IncreaseBoundarySizeByPercentage")," value), or increase the boundary by a percentage of that current boundary size."),Object(b.b)("h2",{id:"settings-breakdown"},"Settings Breakdown"),Object(b.b)("pre",null,Object(b.b)("code",Object(n.a)({parentName:"pre"},{className:"language-json"}),'"ExtendedBoundaries": {\n  "Enable": true,\n  "IncludeContractTypes": [],\n  "ExcludeContractTypes": [],\n  "IncreaseBoundarySizeByPercentage": 0.3,\n  "Overrides": [\n    {\n      "MapId": "mapGeneral_fallenHills_uDeso",\n      "ContractTypeName": "Battle",\n      "IncreaseBoundarySizeByPercentage": 0.4\n    }\n  ]\n},\n')),Object(b.b)("table",null,Object(b.b)("thead",{parentName:"table"},Object(b.b)("tr",{parentName:"thead"},Object(b.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Path"),Object(b.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Required?"),Object(b.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Default"),Object(b.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Example"),Object(b.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Details"))),Object(b.b)("tbody",{parentName:"table"},Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"Enable")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Optional"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"true")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"true")," or ",Object(b.b)("inlineCode",{parentName:"td"},"false")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Should this feature be enabled or not?")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"IncludeContractTypes")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Optional"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"No contract types"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},'["Rescue", "DestroyBase"]')," would limit bounday changes to these two contract types ",Object(b.b)("br",null),Object(b.b)("br",null)," ",Object(b.b)("inlineCode",{parentName:"td"},"[]")," would fallback to default"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"When set, it overrides ",Object(b.b)("inlineCode",{parentName:"td"},"ExcludeContractTypes")," for this level")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"ExcludeContractTypes")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Optional"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"No contract types"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},'["Assasinate", "CaptureBase"]')," would remove these two contract types from the entire list of available contract types. ",Object(b.b)("br",null),Object(b.b)("br",null)," ",Object(b.b)("inlineCode",{parentName:"td"},"[]")," would fallback to default"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Allows you to explicitly exclude boundary changes for all teams for the specified contract types. Not used if ",Object(b.b)("inlineCode",{parentName:"td"},"IncludeContractTypes")," is set")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"IncreaseBoundarySizeByPercentage")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Optional"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"0.2")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"0.1")," 10%, ",Object(b.b)("inlineCode",{parentName:"td"},"1")," max size"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Percentage of the current boundary to increase the boundary by")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"Overrides")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Optional"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"N/A"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"N/A"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Allows for finer grained control of the size increase")))),Object(b.b)("h3",{id:"overrides"},"Overrides"),Object(b.b)("table",null,Object(b.b)("thead",{parentName:"table"},Object(b.b)("tr",{parentName:"thead"},Object(b.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Path"),Object(b.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Required?"),Object(b.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Default"),Object(b.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Details"))),Object(b.b)("tbody",{parentName:"table"},Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"MapId")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Optional"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"N/A"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Map Id to use with contract type combination")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"ContractTypeName")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Optional"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"N/A"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Contract Type Name to use with Map Id")),Object(b.b)("tr",{parentName:"tbody"},Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(b.b)("inlineCode",{parentName:"td"},"IncreaseBoundarySizeByPercentage")),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Optional"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"N/A"),Object(b.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Override for the percentage")))))}p.isMDXComponent=!0},157:function(e,t,a){"use strict";a.d(t,"a",(function(){return c})),a.d(t,"b",(function(){return p}));var n=a(0),r=a.n(n),b=r.a.createContext({}),l=function(e){var t=r.a.useContext(b),a=t;return e&&(a="function"==typeof e?e(t):Object.assign({},t,e)),a},c=function(e){var t=l(e.components);return r.a.createElement(b.Provider,{value:t},e.children)};var i="mdxType",d={inlineCode:"code",wrapper:function(e){var t=e.children;return r.a.createElement(r.a.Fragment,{},t)}},o=Object(n.forwardRef)((function(e,t){var a=e.components,n=e.mdxType,b=e.originalType,c=e.parentName,i=function(e,t){var a={};for(var n in e)Object.prototype.hasOwnProperty.call(e,n)&&-1===t.indexOf(n)&&(a[n]=e[n]);return a}(e,["components","mdxType","originalType","parentName"]),o=l(a),p=n,u=o[c+"."+p]||o[p]||d[p]||b;return a?r.a.createElement(u,Object.assign({},{ref:t},i,{components:a})):r.a.createElement(u,Object.assign({},{ref:t},i))}));function p(e,t){var a=arguments,n=t&&t.mdxType;if("string"==typeof e||n){var b=a.length,l=new Array(b);l[0]=o;var c={};for(var d in t)hasOwnProperty.call(t,d)&&(c[d]=t[d]);c.originalType=e,c[i]="string"==typeof e?e:n,l[1]=c;for(var p=2;p<b;p++)l[p]=a[p];return r.a.createElement.apply(null,l)}return r.a.createElement.apply(null,a)}o.displayName="MDXCreateElement"}}]);