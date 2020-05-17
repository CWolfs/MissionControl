(window.webpackJsonp=window.webpackJsonp||[]).push([[15],{105:function(e,n,t){"use strict";t.r(n),t.d(n,"frontMatter",(function(){return o})),t.d(n,"rightToc",(function(){return l})),t.d(n,"metadata",(function(){return r})),t.d(n,"default",(function(){return c}));var a=t(2),i=(t(0),t(188));const o={id:"release-0.4.3",title:"Release v0.4.3",author:"Richard 'CWolf' Griffiths",author_title:"Developer",author_url:"https://github.com/cwolfs",author_image_url:"https://avatars0.githubusercontent.com/u/7622361",tags:["changelog","release","v0.4.3"]},l=[{value:"Upgrade Instructions",id:"upgrade-instructions",children:[]},{value:"Minor Features",id:"minor-features",children:[]},{value:"Changes",id:"changes",children:[]},{value:"Bugs Fixed",id:"bugs-fixed",children:[]}],r={permalink:"/blog/release-0.4.3",source:"@site/blog\\2020-01-18-release-0.4.3.md",description:"This release is part 2 of a big drive to reduce bugs in Mission Control. It fixes some long standing bugs and annoyances players have.",date:"2020-01-18T00:00:00.000Z",tags:[{label:"changelog",permalink:"/blog/tags/changelog"},{label:"release",permalink:"/blog/tags/release"},{label:"v0.4.3",permalink:"/blog/tags/v-0-4-3"}],title:"Release v0.4.3",prevItem:{title:"Release v0.4.4",permalink:"/blog/release-0.4.4"},nextItem:{title:"Release v0.4.2",permalink:"/blog/release-0.4.2"}},s={rightToc:l,metadata:r};function c({components:e,...n}){return Object(i.b)("wrapper",Object(a.a)({},s,n,{components:e,mdxType:"MDXLayout"}),Object(i.b)("p",null,"This release is part 2 of a big drive to reduce bugs in Mission Control. It fixes some long standing bugs and annoyances players have."),Object(i.b)("p",null,"This turned out to be a much bigger release than I had intended and brings in 27 bug fixes, gameplay balances and minor features."),Object(i.b)("p",null,"Tracked by ",Object(i.b)("a",Object(a.a)({parentName:"p"},{href:"https://github.com/CWolfs/MissionControl/milestone/12?closed=1"}),"Milestone - v0.4.3")),Object(i.b)("h2",{id:"upgrade-instructions"},"Upgrade Instructions"),Object(i.b)("ul",null,Object(i.b)("li",{parentName:"ul"},"If you're using MC without a modpack and none of your own changes, just delete the MC folder and move the new one into your mods folder"),Object(i.b)("li",{parentName:"ul"},"If you're using MC without a modpack but you've made some of your own changes to the configs:",Object(i.b)("ul",{parentName:"li"},Object(i.b)("li",{parentName:"ul"},"Back up your old MC folder"),Object(i.b)("li",{parentName:"ul"},"Move your old MC folder out of your mod folder"),Object(i.b)("li",{parentName:"ul"},"Move the new MC folder into your mod folder"),Object(i.b)("li",{parentName:"ul"},"Manually make the changes back"),Object(i.b)("li",{parentName:"ul"},"If you prefer the old balance of lances, copy your entire ",Object(i.b)("inlineCode",{parentName:"li"},"config/AdditionalLances")," folder back to the new MC mod folder"))),Object(i.b)("li",{parentName:"ul"},"If you're using MC as part of a modpack, let the modpack authors update MC for you")),Object(i.b)("h2",{id:"minor-features"},"Minor Features"),Object(i.b)("ul",null,Object(i.b)("li",{parentName:"ul"},"Additional Lances: Added an option (",Object(i.b)("inlineCode",{parentName:"li"},"MatchAllyLanceCountToEnemy"),") to 'match' ally lance count to the rolled enemy lance count. This is off by default but can be turned on in the ",Object(i.b)("inlineCode",{parentName:"li"},"settings.json"),"."),Object(i.b)("li",{parentName:"ul"},"Additional Lances: Added an option (",Object(i.b)("inlineCode",{parentName:"li"},"IsPrimaryObjectiveIn"),") in the ",Object(i.b)("inlineCode",{parentName:"li"},"settings.json")," to specify if the enemy AL lance(s) should be a primary objective or not."),Object(i.b)("li",{parentName:"ul"},"Additional Lances: Added better friendly lance portraits (Thanks @17783 / Spectre)"),Object(i.b)("li",{parentName:"ul"},"UI: Added an update notification. When a new version of MC is released the Main Menu will show a notification. This can be disabled in the ",Object(i.b)("inlineCode",{parentName:"li"},"settings.json"),"."),Object(i.b)("li",{parentName:"ul"},"AI: Follow Player - Exposed ",Object(i.b)("inlineCode",{parentName:"li"},"TargetZoneRadius")," setting in the ",Object(i.b)("inlineCode",{parentName:"li"},"settings.json"),". This specifies the target zone the following mech will try to enter before not following anymore. If this zone is left, they will try to follow again.")),Object(i.b)("h2",{id:"changes"},"Changes"),Object(i.b)("ul",null,Object(i.b)("li",{parentName:"ul"},"Load Times: Decreased on average between ",Object(i.b)("inlineCode",{parentName:"li"},"30%")," - ",Object(i.b)("inlineCode",{parentName:"li"},"50%")),Object(i.b)("li",{parentName:"ul"},"Additional Lances: Lower tonnage lances can now spawn for higher difficulty contracts (since ",Object(i.b)("inlineCode",{parentName:"li"},"v0.4.2")," only the same class of lance ","[e.g. heavy, assault]"," would spawn for the difficulty level)."),Object(i.b)("li",{parentName:"ul"},"Additional Lances: Reduced max lances to ",Object(i.b)("inlineCode",{parentName:"li"},"1"),". MC is just too brutal at times. This can be edited back under the configs ",Object(i.b)("inlineCode",{parentName:"li"},"MissionControl/config/AdditionalLances/")," if you want it harder again."),Object(i.b)("li",{parentName:"ul"},"Additional Lances: Substantially increased bonus payout from ",Object(i.b)("inlineCode",{parentName:"li"},"20%")," to ",Object(i.b)("inlineCode",{parentName:"li"},"150%"),". This is to try and offset the huge Company cost to fighting extra lances like the repairs, new mechs and pilot recruitment costs."),Object(i.b)("li",{parentName:"ul"},"Extended Boundaries: Increase specific boundary size for contract type & map combinations",Object(i.b)("ul",{parentName:"li"},Object(i.b)("li",{parentName:"ul"},Object(i.b)("inlineCode",{parentName:"li"},"Capture Base")," with ",Object(i.b)("inlineCode",{parentName:"li"},"mapGeneral_frigidSteppes_iTnd")),Object(i.b)("li",{parentName:"ul"},Object(i.b)("inlineCode",{parentName:"li"},"Capture Base")," with ",Object(i.b)("inlineCode",{parentName:"li"},"mapGeneral_icyOutpost_iGlc")),Object(i.b)("li",{parentName:"ul"},Object(i.b)("inlineCode",{parentName:"li"},"Simple Battle")," with ",Object(i.b)("inlineCode",{parentName:"li"},"mapGeneral_highPeak_iGlc")),Object(i.b)("li",{parentName:"ul"},Object(i.b)("inlineCode",{parentName:"li"},"Fire Mission")," (all of them)"))),Object(i.b)("li",{parentName:"ul"},"Extended Boundaries: Increase default boundary increase from ",Object(i.b)("inlineCode",{parentName:"li"},"20%")," to ",Object(i.b)("inlineCode",{parentName:"li"},"30%")),Object(i.b)("li",{parentName:"ul"},"Rescue: Added a minimum distance to objective check for the player lance spawn"),Object(i.b)("li",{parentName:"ul"},"Hot Drop Protection: Changed evasion pips from ",Object(i.b)("inlineCode",{parentName:"li"},"4")," to ",Object(i.b)("inlineCode",{parentName:"li"},"6")," (effectively a change from ",Object(i.b)("inlineCode",{parentName:"li"},"40%")," to ",Object(i.b)("inlineCode",{parentName:"li"},"60%")," evasion) if the starting drop is within enemy detection range"),Object(i.b)("li",{parentName:"ul"},"AI: Follow AI - Reduced 'should sprint' range from ",Object(i.b)("inlineCode",{parentName:"li"},"200")," to ",Object(i.b)("inlineCode",{parentName:"li"},"120")," units. This means the following unit should keep up better with the player mechs")),Object(i.b)("h2",{id:"bugs-fixed"},"Bugs Fixed"),Object(i.b)("ul",null,Object(i.b)("li",{parentName:"ul"},"Random Spawns: Fixed issue where mechs wouldn't be able to move after spawning (stuck in one place from spawn)"),Object(i.b)("li",{parentName:"ul"},"Random Spawns: Fixed issue where mechs would spawn in rocks or buildings"),Object(i.b)("li",{parentName:"ul"},"Random Spawns: Fixed a few issues that could cause long loads"),Object(i.b)("li",{parentName:"ul"},"Random Spawns: Fixed the ",Object(i.b)("inlineCode",{parentName:"li"},"Array out of Bounds")," errors when testing for valid spawn points"),Object(i.b)("li",{parentName:"ul"},"Random Spawns: Fixed issue where the map 'plot' (point of interest) could not be found to orientate spawns on for the map ",Object(i.b)("inlineCode",{parentName:"li"},"mapGeneral_gridLock_uTech")," (and possibly others who shared the same map set up)"),Object(i.b)("li",{parentName:"ul"},"Random Spawns: Fixed issue where the pathfinder mechs would trigger any Region they entered causing an error"),Object(i.b)("li",{parentName:"ul"},"Extended Boundaries: Setting the sub-override ",Object(i.b)("inlineCode",{parentName:"li"},"IncreaseBoundarySizeByPercentage")," was not read. This is a similar issue as ",Object(i.b)("inlineCode",{parentName:"li"},"v0.4.2")," but for the contract type & map ",Object(i.b)("inlineCode",{parentName:"li"},"Overrides")," section instead."),Object(i.b)("li",{parentName:"ul"},"Extended Boundaries: Customised size position can be wrong causing key points outside of boundary (same bug as in ",Object(i.b)("inlineCode",{parentName:"li"},"v0.4.2"),"). Should be properly fixed now."),Object(i.b)("li",{parentName:"ul"},"Localisation: Fixed a possible issue with locales that don't use period / fullstop for their decimal place. An error would occur when loading a map with an Additional Lance reward"),Object(i.b)("li",{parentName:"ul"},"Additional Lances: Fixed issue where if the player restarted a contract 3 or more times then no more AL lances would spawn (friendly or enemy)"),Object(i.b)("li",{parentName:"ul"},"Additional Lances: Fixed issue where Heavy Metal campaign contracts would allow Mission Control to run (and Bigger Drops). There's a setting ",Object(i.b)("inlineCode",{parentName:"li"},"(DisableIfFlashpointContract)")," in the ",Object(i.b)("inlineCode",{parentName:"li"},"settings.json")," to allow for MC to run in flashpoints but it's off by default.")))}c.isMDXComponent=!0},188:function(e,n,t){"use strict";t.d(n,"a",(function(){return r})),t.d(n,"b",(function(){return d}));var a=t(0),i=t.n(a),o=i.a.createContext({}),l=function(e){var n=i.a.useContext(o),t=n;return e&&(t="function"==typeof e?e(n):Object.assign({},n,e)),t},r=function(e){var n=l(e.components);return i.a.createElement(o.Provider,{value:n},e.children)};var s={inlineCode:"code",wrapper:function(e){var n=e.children;return i.a.createElement(i.a.Fragment,{},n)}},c=Object(a.forwardRef)((function(e,n){var t=e.components,a=e.mdxType,o=e.originalType,r=e.parentName,c=function(e,n){var t={};for(var a in e)Object.prototype.hasOwnProperty.call(e,a)&&-1===n.indexOf(a)&&(t[a]=e[a]);return t}(e,["components","mdxType","originalType","parentName"]),d=l(t),b=a,u=d[r+"."+b]||d[b]||s[b]||o;return t?i.a.createElement(u,Object.assign({},{ref:n},c,{components:t})):i.a.createElement(u,Object.assign({},{ref:n},c))}));function d(e,n){var t=arguments,a=n&&n.mdxType;if("string"==typeof e||a){var o=t.length,l=new Array(o);l[0]=c;var r={};for(var s in n)hasOwnProperty.call(n,s)&&(r[s]=n[s]);r.originalType=e,r.mdxType="string"==typeof e?e:a,l[1]=r;for(var d=2;d<o;d++)l[d]=t[d];return i.a.createElement.apply(null,l)}return i.a.createElement.apply(null,t)}c.displayName="MDXCreateElement"}}]);