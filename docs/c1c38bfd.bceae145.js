(window.webpackJsonp=window.webpackJsonp||[]).push([[80],{169:function(e,t,a){"use strict";a.r(t),a.d(t,"frontMatter",(function(){return o})),a.d(t,"rightToc",(function(){return l})),a.d(t,"metadata",(function(){return s})),a.d(t,"default",(function(){return u}));var n=a(2),r=a(6),i=(a(0),a(188)),o={id:"release-0.3.2",title:"Release v0.3.2",author:"Richard 'CWolf' Griffiths",author_title:"Developer",author_url:"https://github.com/cwolfs",author_image_url:"https://avatars0.githubusercontent.com/u/7622361",tags:["changelog","release","v0.3.2"]},l=[{value:"Major Features",id:"major-features",children:[]},{value:"Minor Features",id:"minor-features",children:[]},{value:"Bugs Fixed",id:"bugs-fixed",children:[]}],s={permalink:"/blog/release-0.3.2",source:"@site/blog\\2019-11-30-release-0.3.2.md",description:"This release brings in BT 1.8 compatibility and some features and bugfixes. Sadly, I haven't been able to fix the spawn and load issues yet but I am working on it.",date:"2019-11-30T00:00:00.000Z",tags:[{label:"changelog",permalink:"/blog/tags/changelog"},{label:"release",permalink:"/blog/tags/release"},{label:"v0.3.2",permalink:"/blog/tags/v-0-3-2"}],title:"Release v0.3.2",prevItem:{title:"Welcome",permalink:"/blog/welcome"},nextItem:{title:"Release v0.3.1",permalink:"/blog/release-0.3.1"}},c={rightToc:l,metadata:s};function u(e){var t=e.components,a=Object(r.a)(e,["components"]);return Object(i.b)("wrapper",Object(n.a)({},c,a,{components:t,mdxType:"MDXLayout"}),Object(i.b)("p",null,"This release brings in BT 1.8 compatibility and some features and bugfixes. Sadly, I haven't been able to fix the spawn and load issues yet but I am working on it."),Object(i.b)("p",null,"Tracked by ",Object(i.b)("a",Object(n.a)({parentName:"p"},{href:"https://github.com/CWolfs/MissionControl/milestone/9"}),"Milestone - v0.3.2")),Object(i.b)("h2",{id:"major-features"},"Major Features"),Object(i.b)("ul",null,Object(i.b)("li",{parentName:"ul"},"Added BattleTech 1.8 support"),Object(i.b)("li",{parentName:"ul"},"Additional Lances: Added contract rewards for destroying Additional Lance enemy lances.",Object(i.b)("ul",{parentName:"li"},Object(i.b)("li",{parentName:"ul"},"Read the ",Object(i.b)("a",Object(n.a)({parentName:"li"},{href:"https://github.com/CWolfs/MissionControl/blob/master/docs/additional-lances.md"}),"AL documentation")," for the ",Object(i.b)("inlineCode",{parentName:"li"},"RewardsPerLance")," section. Default is 20% contract value reward."))),Object(i.b)("li",{parentName:"ul"},"Extended Boundaries: Provided more control on the size of the encounter boundaries",Object(i.b)("ul",{parentName:"li"},Object(i.b)("li",{parentName:"ul"},"Read the ",Object(i.b)("a",Object(n.a)({parentName:"li"},{href:"https://github.com/CWolfs/MissionControl/blob/master/docs/extended-boundaries.md"}),"EB documentarion")," for how to control the boundary size."))),Object(i.b)("li",{parentName:"ul"},"Added ability to prevent specific Additional Lance lance configs from autofilling with Extended Lance autofill",Object(i.b)("ul",{parentName:"li"},Object(i.b)("li",{parentName:"ul"},"Read the ",Object(i.b)("a",Object(n.a)({parentName:"li"},{href:"https://github.com/CWolfs/MissionControl/blob/master/docs/additional-lances.md"}),"AL documentation")," for the ",Object(i.b)("inlineCode",{parentName:"li"},"supportAutofill")," property.")))),Object(i.b)("h2",{id:"minor-features"},"Minor Features"),Object(i.b)("ul",null,Object(i.b)("li",{parentName:"ul"},"Random Spawns: Provided a setting to turn off random spawns for the original map spawns (Additional Lances and Extended Lances will still spawn according to their spawn profile even with this turned to ",Object(i.b)("inlineCode",{parentName:"li"},"false"),")")),Object(i.b)("h2",{id:"bugs-fixed"},"Bugs Fixed"),Object(i.b)("ul",null,Object(i.b)("li",{parentName:"ul"},"Quick Skirmish: Launching into a Quick Skirmish is now fixed. Thanks ",Object(i.b)("a",Object(n.a)({parentName:"li"},{href:"https://github.com/CMiSSioN"}),"KMiSSioN")),Object(i.b)("li",{parentName:"ul"},"Quick Skmirish: Completing a Quick Skirmish fight is now fixed.")))}u.isMDXComponent=!0},188:function(e,t,a){"use strict";a.d(t,"a",(function(){return l})),a.d(t,"b",(function(){return u}));var n=a(0),r=a.n(n),i=r.a.createContext({}),o=function(e){var t=r.a.useContext(i),a=t;return e&&(a="function"==typeof e?e(t):Object.assign({},t,e)),a},l=function(e){var t=o(e.components);return r.a.createElement(i.Provider,{value:t},e.children)};var s={inlineCode:"code",wrapper:function(e){var t=e.children;return r.a.createElement(r.a.Fragment,{},t)}},c=Object(n.forwardRef)((function(e,t){var a=e.components,n=e.mdxType,i=e.originalType,l=e.parentName,c=function(e,t){var a={};for(var n in e)Object.prototype.hasOwnProperty.call(e,n)&&-1===t.indexOf(n)&&(a[n]=e[n]);return a}(e,["components","mdxType","originalType","parentName"]),u=o(a),b=n,d=u[l+"."+b]||u[b]||s[b]||i;return a?r.a.createElement(d,Object.assign({},{ref:t},c,{components:a})):r.a.createElement(d,Object.assign({},{ref:t},c))}));function u(e,t){var a=arguments,n=t&&t.mdxType;if("string"==typeof e||n){var i=a.length,o=new Array(i);o[0]=c;var l={};for(var s in t)hasOwnProperty.call(t,s)&&(l[s]=t[s]);l.originalType=e,l.mdxType="string"==typeof e?e:n,o[1]=l;for(var u=2;u<i;u++)o[u]=a[u];return r.a.createElement.apply(null,o)}return r.a.createElement.apply(null,a)}c.displayName="MDXCreateElement"}}]);