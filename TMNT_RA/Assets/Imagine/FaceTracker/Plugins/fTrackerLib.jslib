mergeInto(LibraryManager.library, {

	StartWebGLfTracker: function(name)
	{
        if(!window.fTracker){
            console.error('%cfTracker not found! Please make sure to use the fTracker WebGLTemplate in your ProjectSettings','font-size: 32px; font-weight: bold');
            throw new Error("Tracker not found! Please make sure to use the fTracker WebGLTemplate in your ProjectSettings");
            return;
        }

    	window.fTracker.startTracker(UTF8ToString(name));
    },
    StopWebGLfTracker: function()
	{
    	window.fTracker.stopTracker();
    },
    IsWebGLfTrackerReady: function()
    {
        return window.fTracker != null;
    },
    SetWebGLfTrackerSettings: function(settings)
	{
    	window.fTracker.setTrackerSettings(UTF8ToString(settings), "1.1.0.230332");
    },
});
