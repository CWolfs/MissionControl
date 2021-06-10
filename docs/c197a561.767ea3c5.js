(window.webpackJsonp=window.webpackJsonp||[]).push([[129],{218:function(e,t,n){"use strict";n.r(t),n.d(t,"frontMatter",(function(){return l})),n.d(t,"rightToc",(function(){return b})),n.d(t,"metadata",(function(){return c})),n.d(t,"default",(function(){return u}));var a=n(2),r=n(6),i=(n(0),n(251)),l={id:"tag-units-in-region-result",title:"Tag Units In Region Result",sidebar_label:"Tag Units In Region"},b=[{value:"Properties",id:"properties",children:[]},{value:"Example",id:"example",children:[]}],c={id:"contract-builder-api/trigger-results/tag-units-in-region-result",title:"Tag Units In Region Result",description:"The `TagUnitsInRegion` result adds tags to units within a region.\r",source:"@site/docs\\contract-builder-api\\trigger-results\\tag-units-in-region-result.md",permalink:"/docs/contract-builder-api/trigger-results/tag-units-in-region-result",sidebar_label:"Tag Units In Region",sidebar:"docs",previous:{title:"Set Units in Region to be Tagged Objective Targets Result",permalink:"/docs/contract-builder-api/trigger-results/set-units-in-region-to-be-tagged-objective-targets-result"},next:{title:"Trigger Result At Random Result",permalink:"/docs/contract-builder-api/trigger-results/trigger-result-at-random-result"}},o={rightToc:b,metadata:c};function u(e){var t=e.components,n=Object(r.a)(e,["components"]);return Object(i.b)("wrapper",Object(a.a)({},o,n,{components:t,mdxType:"MDXLayout"}),Object(i.b)("p",null,"The ",Object(i.b)("inlineCode",{parentName:"p"},"TagUnitsInRegion")," result adds tags to units within a region."),Object(i.b)("p",null,"Currently only ",Object(i.b)("inlineCode",{parentName:"p"},"Building")," type is supported for this result."),Object(i.b)("h2",{id:"properties"},"Properties"),Object(i.b)("table",null,Object(i.b)("thead",{parentName:"table"},Object(i.b)("tr",{parentName:"thead"},Object(i.b)("th",Object(a.a)({parentName:"tr"},{align:null}),"Property"),Object(i.b)("th",Object(a.a)({parentName:"tr"},{align:null}),"Required"),Object(i.b)("th",Object(a.a)({parentName:"tr"},{align:null}),"Default"),Object(i.b)("th",Object(a.a)({parentName:"tr"},{align:null}),"Details"))),Object(i.b)("tbody",{parentName:"table"},Object(i.b)("tr",{parentName:"tbody"},Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),"Type"),Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),"true"),Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),Object(i.b)("inlineCode",{parentName:"td"},"TagUnitsInRegion")),Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),"-")),Object(i.b)("tr",{parentName:"tbody"},Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),"RegionGuid"),Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),"true"),Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),"-"),Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),"Guid of the ",Object(i.b)("inlineCode",{parentName:"td"},"Region"))),Object(i.b)("tr",{parentName:"tbody"},Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),"UnitType"),Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),"true"),Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),"-"),Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),"Type of Unit to tag.",Object(i.b)("br",null),Object(i.b)("br",null),"Current supports: ",Object(i.b)("inlineCode",{parentName:"td"},"Building"))),Object(i.b)("tr",{parentName:"tbody"},Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),"NumberOfUnits"),Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),"false"),Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),Object(i.b)("inlineCode",{parentName:"td"},"1")),Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),"Number of units to tag")),Object(i.b)("tr",{parentName:"tbody"},Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),"Tags"),Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),"true"),Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),"-"),Object(i.b)("td",Object(a.a)({parentName:"tr"},{align:null}),"Tags to add to units")))),Object(i.b)("h2",{id:"example"},"Example"),Object(i.b)("pre",null,Object(i.b)("code",Object(a.a)({parentName:"pre"},{className:"language-json"}),'{\n  "Type": "TagUnitsInRegion",\n  "RegionGuid": "21a03616-c88b-4edd-a9a9-b4dd54b46d6c",\n  "UnitType": "Building",\n  "NumberOfUnits": 4,\n  "Tags": ["defend_building_3b"]\n}\n')))}u.isMDXComponent=!0},251:function(e,t,n){"use strict";n.d(t,"a",(function(){return b})),n.d(t,"b",(function(){return u}));var a=n(0),r=n.n(a),i=r.a.createContext({}),l=function(e){var t=r.a.useContext(i),n=t;return e&&(n="function"==typeof e?e(t):Object.assign({},t,e)),n},b=function(e){var t=l(e.components);return r.a.createElement(i.Provider,{value:t},e.children)};var c={inlineCode:"code",wrapper:function(e){var t=e.children;return r.a.createElement(r.a.Fragment,{},t)}},o=Object(a.forwardRef)((function(e,t){var n=e.components,a=e.mdxType,i=e.originalType,b=e.parentName,o=function(e,t){var n={};for(var a in e)Object.prototype.hasOwnProperty.call(e,a)&&-1===t.indexOf(a)&&(n[a]=e[a]);return n}(e,["components","mdxType","originalType","parentName"]),u=l(n),p=a,d=u[b+"."+p]||u[p]||c[p]||i;return n?r.a.createElement(d,Object.assign({},{ref:t},o,{components:n})):r.a.createElement(d,Object.assign({},{ref:t},o))}));function u(e,t){var n=arguments,a=t&&t.mdxType;if("string"==typeof e||a){var i=n.length,l=new Array(i);l[0]=o;var b={};for(var c in t)hasOwnProperty.call(t,c)&&(b[c]=t[c]);b.originalType=e,b.mdxType="string"==typeof e?e:a,l[1]=b;for(var u=2;u<i;u++)l[u]=n[u];return r.a.createElement.apply(null,l)}return r.a.createElement.apply(null,n)}o.displayName="MDXCreateElement"}}]);