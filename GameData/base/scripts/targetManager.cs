//--------------------------------------------------------------------------
// helper function for creating targets
function createTarget(%obj, %nameTag, %skinTag, %voiceTag, %typeTag, %sensorGroup, %voicePitch)
{
    if (%voicePitch $= "" || %voicePitch == 0)
        %voicePitch = 1.0;
    %data = (%obj.getType() & $TypeMasks::ShapeBaseObjectType) ? %obj.getDataBlock() : 0;
    %target = allocTarget(%nameTag, %skinTag, %voiceTag, %typeTag,
        %sensorGroup, %data, %voicePitch);

    %obj.setTarget(%target);
    return(%target);
}

//--------------------------------------------------------------------------
// useful for when a client switches teams or joins the game
function clientResetTargets(%client, %tasksOnly)
{
    if (%client.isAiControlled())
        return;

    // remove just tasks or everything?
    resetClientTargets(%client, %tasksOnly);

    // notify the client to cleanup the gui...
    commandToClient(%client, 'ResetTaskList');
}

//--------------------------------------------------------------------------
// useful at end of missions
function resetTargetManager()
{
    %count = ClientGroup.getCount();

    // clear the lookup table
    for (%cl = 0; %cl < %count; %cl++)
    {
        %client = ClientGroup.getObject(%cl);
        $TargetToClient[%client.target] = "";
    }

    // reset all the targets on all the connections
    resetTargets();

    // create targets for all the clients
    for (%cl = 0; %cl < %count; %cl++)
    {
        %client = ClientGroup.getObject(%cl);

        if (!%client.isAiControlled())
            commandToClient(%client, 'ResetTaskList');

        // reset the clients target and update the lookup table
        %client.target = allocClientTarget(%client, %client.name, %client.skin,
            %client.voiceTag, '_ClientConnection', 0, 0, %client.voicePitch);
    }
}

//--------------------------------------------------------------------------
// wrap the client targets to maintain a lookup table on the server
function allocClientTarget(%client, %nameTag, %skinTag, %voiceTag, %typeTag,
        %sensorGroup, %datablock, %voicePitch)
{
    if (%voicePitch $= "" || %voicePitch == 0)
        %voicePitch = 1.0;
    echo("allocating client target - skin = " @ getTaggedString(%skinTag));
    %target = allocTarget(%nameTag, %skinTag, %voiceTag, %typeTag, %sensorGroup,
        %datablock, %voicePitch, %skinTag);

    // first bit is the triangle
    setTargetRenderMask(%target, (1 << $TargetInfo::HudRenderStart));

    $TargetToClient[%target] = %client;
    return(%target);
}

function freeClientTarget(%client)
{
    $TargetToClient[%client.target] = "";
    freeTarget(%client.target);
}

//--------------------------------------------------------------------------
function ClientTarget::onAdd(%this, %type)
{
    %this.client = TaskList.currentTaskClient;
    %this.AIObjective = TaskList.currentAIObjective;
    %this.team = TaskList.currentTaskIsTeam;
    %this.description = TaskList.currentTaskDescription;

    %player = $PlayerList[%this.client];
    %this.clientName = %player ? %player.name : "[Unknown]";

    switch$(%type)
    {
    case "AssignedTask":
        TaskList.currentTask = %this;
        %this.setText(%this.description);

    case "PotentialTask":
        // add to the task list and display a message
        TaskList.addTask(%this, %this.clientName, detag(%this.description));
        %this.setText(%this.description);
    }
}

function ClientTarget::onDie(%this, %type)
{
    // if this target is not removed from its current group then it will be
    // deleted on return of this call
    switch$(%type)
    {
    case "AssignedTask": // let it die
        TaskList.currentTask = -1;

    case "PotentialTask":
        TaskList.handleDyingTask(%this);
    }
}
