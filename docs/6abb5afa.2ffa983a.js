(window.webpackJsonp=window.webpackJsonp||[]).push([[27],{117:function(e,t,n){"use strict";n.r(t),n.d(t,"frontMatter",(function(){return l})),n.d(t,"rightToc",(function(){return o})),n.d(t,"metadata",(function(){return s})),n.d(t,"default",(function(){return p}));var a=n(1),r=n(6),i=(n(0),n(157)),l={id:"release-0.2.2",title:"Release v0.2.2",author:"Richard 'CWolf' Griffiths",author_title:"Developer",author_url:"https://github.com/cwolfs",author_image_url:"https://avatars0.githubusercontent.com/u/7622361",tags:["changelog","release","v0.2.2"]},o=[{value:"Improvements",id:"improvements",children:[]},{value:"Bugs Fixed",id:"bugs-fixed",children:[]}],s={permalink:"/blog/release-0.2.2",source:"@site/blog\\2019-06-21-release-0.2.3.md",description:"This release focuses on bug fixing and stabilising Mission Control (as it's well known for its long loads and patchy spawn points).",date:"2019-06-21T00:00:00.000Z",tags:[{label:"changelog",permalink:"/blog/tags/changelog"},{label:"release",permalink:"/blog/tags/release"},{label:"v0.2.2",permalink:"/blog/tags/v-0-2-2"}],title:"Release v0.2.2",prevItem:{title:"Release v0.2.2",permalink:"/blog/release-0.2.2"},nextItem:{title:"Release v0.2.1",permalink:"/blog/release-0.2.1"}},c={rightToc:o,metadata:s},u="wrapper";function p(e){var t=e.components,n=Object(r.a)(e,["components"]);return Object(i.b)(u,Object(a.a)({},c,n,{components:t,mdxType:"MDXLayout"}),Object(i.b)("p",null,"This release focuses on bug fixing and stabilising Mission Control (as it's well known for its long loads and patchy spawn points)."),Object(i.b)("h2",{id:"improvements"},"Improvements"),Object(i.b)("ul",null,Object(i.b)("li",{parentName:"ul"},"Map load times are much faster, even in a worst case situation.")),Object(i.b)("h2",{id:"bugs-fixed"},"Bugs Fixed"),Object(i.b)("ul",null,Object(i.b)("li",{parentName:"ul"},"Fixed infinite load screen wait bugs"),Object(i.b)("li",{parentName:"ul"},"Fixed unit spawns that didn't allow a unit to move"),Object(i.b)("li",{parentName:"ul"},"Fixed units spawning on buildings (urban maps highlighted this bug)"),Object(i.b)("li",{parentName:"ul"},"Fixed 'Follow Player' AI - Between BTv1.4 and BTv1.6.2 a tag was changed that broke this AI")))}p.isMDXComponent=!0},157:function(e,t,n){"use strict";n.d(t,"a",(function(){return o})),n.d(t,"b",(function(){return p}));var a=n(0),r=n.n(a),i=r.a.createContext({}),l=function(e){var t=r.a.useContext(i),n=t;return e&&(n="function"==typeof e?e(t):Object.assign({},t,e)),n},o=function(e){var t=l(e.components);return r.a.createElement(i.Provider,{value:t},e.children)};var s="mdxType",c={inlineCode:"code",wrapper:function(e){var t=e.children;return r.a.createElement(r.a.Fragment,{},t)}},u=Object(a.forwardRef)((function(e,t){var n=e.components,a=e.mdxType,i=e.originalType,o=e.parentName,s=function(e,t){var n={};for(var a in e)Object.prototype.hasOwnProperty.call(e,a)&&-1===t.indexOf(a)&&(n[a]=e[a]);return n}(e,["components","mdxType","originalType","parentName"]),u=l(n),p=a,d=u[o+"."+p]||u[p]||c[p]||i;return n?r.a.createElement(d,Object.assign({},{ref:t},s,{components:n})):r.a.createElement(d,Object.assign({},{ref:t},s))}));function p(e,t){var n=arguments,a=t&&t.mdxType;if("string"==typeof e||a){var i=n.length,l=new Array(i);l[0]=u;var o={};for(var c in t)hasOwnProperty.call(t,c)&&(o[c]=t[c]);o.originalType=e,o[s]="string"==typeof e?e:a,l[1]=o;for(var p=2;p<i;p++)l[p]=n[p];return r.a.createElement.apply(null,l)}return r.a.createElement.apply(null,n)}u.displayName="MDXCreateElement"}}]);