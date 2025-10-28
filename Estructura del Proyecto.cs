Tengo esta advertencia:

main.ts:5 NG0505: Angular hydration was requested on the client, but there was no serialized information present in the server response, thus hydration was not enabled. Make sure the `provideClientHydration()` is included into the list of providers in the server part of the application configuration. Find more at https://angular.dev/errors/NG0505
warn	@	debug_node.mjs:17994
useValue	@	core.mjs:3731
resolveInjectorInitializers	@	root_effect_scheduler.mjs:2073
(anonymous)	@	core.mjs:906
invoke	@	zone.js:398
onInvoke	@	debug_node.mjs:16672
invoke	@	zone.js:397
run	@	zone.js:113
run	@	debug_node.mjs:16518
bootstrap	@	core.mjs:904
internalCreateApplication	@	core.mjs:2659
(anonymous)	@	browser.mjs:440
invoke	@	zone.js:398
run	@	zone.js:113
(anonymous)	@	zone.js:2537
invokeTask	@	zone.js:431
runTask	@	zone.js:161
drainMicroTaskQueue	@	zone.js:612
Promise.then		
nativeScheduleMicroTask	@	zone.js:588
scheduleMicroTask	@	zone.js:599
scheduleTask	@	zone.js:420
scheduleTask	@	zone.js:207
scheduleMicroTask	@	zone.js:227
scheduleResolveOrReject	@	zone.js:2527
then	@	zone.js:2732
resolveComponentResources	@	debug_node.mjs:14689
bootstrapApplication	@	browser.mjs:437
(anonymous)	@	main.ts:5

