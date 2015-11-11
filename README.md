# CCNetEnhancedBuildPublisher
An enhanced build publisher for Cruise Control .NET that offers a new property indicating if the publisher should recurse directories.

This publisher operates in all ways, exactly like the standard BuildPublisher.

Use the following optional tag within the EnhancedBuildPublisher tag to allow it to recurse through subfolders.
&lt;recurse&gt;true&lt;/recurse&gt;

Specify false to only copy files at the top level and skip subfolders.

The default is "true" if left out, allowing it to operate like the original BuildPublisher task.
