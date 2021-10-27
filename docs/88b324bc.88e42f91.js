(window.webpackJsonp=window.webpackJsonp||[]).push([[92],{182:function(e,t,a){"use strict";a.r(t),a.d(t,"frontMatter",(function(){return i})),a.d(t,"rightToc",(function(){return c})),a.d(t,"metadata",(function(){return o})),a.d(t,"default",(function(){return p}));var n=a(2),r=a(6),l=(a(0),a(263)),i={id:"plots",title:"Plots",sidebar_label:"Plots"},c=[{value:"Example",id:"example",children:[]}],o={id:"contract-builder-api/plots",title:"Plots",description:"`Plots` are typically collections of buildings, or bases that help make vanilla maps seem a bit more dynamic. They are turned on and off by the HBS designer when they created each contract type on a map. You can control which plots are enabled or disabled.\r",source:"@site/docs\\contract-builder-api\\plots.md",permalink:"/docs/contract-builder-api/plots",sidebar_label:"Plots",sidebar:"docs",previous:{title:"Overrides",permalink:"/docs/contract-builder-api/overrides"},next:{title:"Triggers",permalink:"/docs/contract-builder-api/triggers"}},b={rightToc:c,metadata:o};function p(e){var t=e.components,a=Object(r.a)(e,["components"]);return Object(l.b)("wrapper",Object(n.a)({},b,a,{components:t,mdxType:"MDXLayout"}),Object(l.b)("p",null,Object(l.b)("inlineCode",{parentName:"p"},"Plots")," are typically collections of buildings, or bases that help make vanilla maps seem a bit more dynamic. They are turned on and off by the HBS designer when they created each contract type on a map. You can control which plots are enabled or disabled."),Object(l.b)("p",null,"Since ",Object(l.b)("inlineCode",{parentName:"p"},"Plots")," are always map specific, then any ",Object(l.b)("inlineCode",{parentName:"p"},"Plot")," control is made in the map override file. Read the ",Object(l.b)("a",Object(n.a)({parentName:"p"},{href:"/docs/contract-builder-api/overrides"}),"Overrides API")," section for detailed information."),Object(l.b)("h2",{id:"example"},"Example"),Object(l.b)("pre",null,Object(l.b)("code",Object(n.a)({parentName:"pre"},{className:"language-json"}),'"Overrides": [\n    {\n      "Path": "$",\n      "Action": "ObjectMerge",\n      "Value": {\n        "Plots": [\n          {\n            "Name": "Cresttop Structures",\n            "Variant": "plotVariant_facilityMedTowerWarehouse"\n          },\n          {\n            "Name": "Valley Fort",\n            "Variant": "plotVariant_facilityLrgMilitaryAirControlBase"\n          }\n        ]\n      }\n    },\n    // more map specific override data\n]\n')),Object(l.b)("table",null,Object(l.b)("thead",{parentName:"table"},Object(l.b)("tr",{parentName:"thead"},Object(l.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Property"),Object(l.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Required"),Object(l.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Default"),Object(l.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Details"))),Object(l.b)("tbody",{parentName:"table"},Object(l.b)("tr",{parentName:"tbody"},Object(l.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Plots"),Object(l.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"false"),Object(l.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"-"),Object(l.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Specifying you want to start defining which plots to enable in the map")))),Object(l.b)("table",null,Object(l.b)("thead",{parentName:"table"},Object(l.b)("tr",{parentName:"thead"},Object(l.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Property"),Object(l.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Required"),Object(l.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Default"),Object(l.b)("th",Object(n.a)({parentName:"tr"},{align:null}),"Details"))),Object(l.b)("tbody",{parentName:"table"},Object(l.b)("tr",{parentName:"tbody"},Object(l.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Name"),Object(l.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"true"),Object(l.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"-"),Object(l.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Specifies the name of the ",Object(l.b)("inlineCode",{parentName:"td"},"Plot")," you want to turn on. You can find this with BTDebug under the ",Object(l.b)("inlineCode",{parentName:"td"},"Plots")," object")),Object(l.b)("tr",{parentName:"tbody"},Object(l.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Variant"),Object(l.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"false"),Object(l.b)("td",Object(n.a)({parentName:"tr"},{align:null}),Object(l.b)("inlineCode",{parentName:"td"},"Default")),Object(l.b)("td",Object(n.a)({parentName:"tr"},{align:null}),"Specifies the variant of the ",Object(l.b)("inlineCode",{parentName:"td"},"Plot")," you defined in ",Object(l.b)("inlineCode",{parentName:"td"},"Name"),". Sometimes there are multiple versions of a certain plot. There is often a default ",Object(l.b)("inlineCode",{parentName:"td"},"Plot")," variant called ",Object(l.b)("inlineCode",{parentName:"td"},"Default")," but it's usually best to define what you want")))))}p.isMDXComponent=!0},263:function(e,t,a){"use strict";a.d(t,"a",(function(){return c})),a.d(t,"b",(function(){return p}));var n=a(0),r=a.n(n),l=r.a.createContext({}),i=function(e){var t=r.a.useContext(l),a=t;return e&&(a="function"==typeof e?e(t):Object.assign({},t,e)),a},c=function(e){var t=i(e.components);return r.a.createElement(l.Provider,{value:t},e.children)};var o={inlineCode:"code",wrapper:function(e){var t=e.children;return r.a.createElement(r.a.Fragment,{},t)}},b=Object(n.forwardRef)((function(e,t){var a=e.components,n=e.mdxType,l=e.originalType,c=e.parentName,b=function(e,t){var a={};for(var n in e)Object.prototype.hasOwnProperty.call(e,n)&&-1===t.indexOf(n)&&(a[n]=e[n]);return a}(e,["components","mdxType","originalType","parentName"]),p=i(a),d=n,s=p[c+"."+d]||p[d]||o[d]||l;return a?r.a.createElement(s,Object.assign({},{ref:t},b,{components:a})):r.a.createElement(s,Object.assign({},{ref:t},b))}));function p(e,t){var a=arguments,n=t&&t.mdxType;if("string"==typeof e||n){var l=a.length,i=new Array(l);i[0]=b;var c={};for(var o in t)hasOwnProperty.call(t,o)&&(c[o]=t[o]);c.originalType=e,c.mdxType="string"==typeof e?e:n,i[1]=c;for(var p=2;p<l;p++)i[p]=a[p];return r.a.createElement.apply(null,i)}return r.a.createElement.apply(null,a)}b.displayName="MDXCreateElement"}}]);