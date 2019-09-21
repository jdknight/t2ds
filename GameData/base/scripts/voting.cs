//------------------------------------------------------------------------------
//
// Voting-related Implementation
//
//------------------------------------------------------------------------------

if(!isObject(Vote))
{
    new ScriptObject(Vote);
    $VotingRestricted = false;
}

function serverCmdStartNewVote(%client, %type, %arg1, %arg2, %arg3, %arg4)
{
    if (!isObject(Game) || $VotingRestricted)
        return;

    // validate request type
    %checkTarget = false;
    switch$ (%type)
    {
    case "BanPlayer":
        if (!%client.isSuperAdmin)
            return;
        %checkTarget = true;

    case "VoteAdminPlayer":
        if (!$Host::AllowAdminPlayerVotes && !%client.isSuperAdmin)
            return;
        %checkTarget = true;

    case "VoteRebootServer":
        if (!%client.isSuperAdmin || !$Host::AllowSuperAdminServerReboot)
            return;

    case "VoteKickPlayer":
        %checkTarget = true;

    default:
        if (!Game.isValidVote(%client, %type, %arg1, %arg2, %arg3, %arg4))
            return;
    }

    // ignore if client cannot vote
    %admin = (%client.isAdmin || %client.isSuperAdmin ? %client : 0);
    if (!%client.canVote && %admin == 0)
        return;

    // if this request is targeting another client, ensure they exist
    %targetAddress = "";
    %targetClient = 0;
    %targetClientName = "";
    %targetGuid = 0;
    %targetTeam = 0;
    if (%checkTarget)
    {
        for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
        {
            %cl = ClientGroup.getObject(%idx);
            if (%cl == %arg1)
            {
                %targetAddress = %cl.getAddress();
                %targetClient = %cl;
                %targetClientName = %cl.name;
                %targetGuid = %cl.guid;
                %targetTeam = %cl.team;
                break;
            }
        }
        if (%targetClient == 0) return;
    }

    // pre-process/filter requests
    %teamSpecific = false;
    switch$ (%type)
    {
    case "BanPlayer":
        // ignore self-bans and targets who are super administrators
        if (%client == %targetClient || %targetClient.isSuperAdmin)
            return;

        ban(%targetClient, %client);
        return;

    case "VoteAdminPlayer":
        // ignore if target is already an administrator
        if (%targetClient.isAdmin)
            return;

        if (%admin > 0)
        {
            // check if this request conflicts with pending a vote
            if (Vote.targetClient == %targetClient && (
                    Vote.type $= "VoteAdminPlayer" ||
                    Vote.type $= "VoteKickPlayer"))
                clearVotes();

            messageAll('MsgAdminAdminPlayer', '\c2%3 made %2 an administrator.',
                %targetClient, %targetClientName, %client.name);
            %targetClient.isAdmin = true;
            return;
        }

    case "VoteRebootServer":
        // abort any active vote on reset
        if (Vote.type !$= "")
            clearVotes();

        $VotingRestricted = true;
        Game.gameOver();
        messageAll('MsgServerRestart',
            '\c2Administrator triggered a server reboot.~wfx/misc/red_alert.wav');
        error(%client.nameBase SPC "triggered a server shutdown.");
        schedule(10000, 0, rebootServer);
        return;

    case "VoteKickPlayer":
        // never allow an administrator to be kicked except by equivalent access
        if ((%targetClient.isSuperAdmin && !%client.isSuperAdmin) ||
                (%targetClient.isAdmin && !%client.isAdmin))
            return;

        // ignore self-kicks
        if (%client == %targetClient)
            return;

        if (%admin > 0)
        {
            // check if this request was pending a vote
            if (Vote.type $= "VoteKickPlayer" && Vote.targetClient == %targetClient)
                clearVotes();

            kick(%targetClient, %admin,
                %targetGuid, %targetAddress, %targetClientName, %targetTeam);
            return;
        }

        // non-administrator vote-kicks must be team-based
        %teamSpecific = (Game.numTeams > 1 && %targetClient.team != 0);
        if (%teamSpecific && %client.team != %targetClient.team)
            return;

    default:
        if (Game.preprocessVote(%client, %type, %arg1, %arg2, %arg3, %arg4))
            return;
    }

    // this is a vote to be scheduled
    if (Vote.scheduled !$= "")
    {
        messageClient(%client, 'voteAlreadyRunning',
            '\c2A vote is already in progress.');
        return;
    }

    clearVotes();
    %clientsVoting = 0;

    for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
    {
        %cl = ClientGroup.getObject(%idx);
        if (%cl.isAIControlled()) continue;

        switch$ (%type)
        {
        case "VoteAdminPlayer":
            if (%teamSpecific && %cl.team != %client.team) continue;

            if (%client != %targetClient)
                messageClient(%cl, 'VoteStarted',
                    '\c2%1 initiated a vote to make \c5%2\c2 an administrator.',
                    %client.name, %targetClientName);
            else
                messageClient(%cl, 'VoteStarted',
                    '\c2%1 initiated a vote to make themselves an administrator.',
                    %client.name);

        case "VoteKickPlayer":
            if (%teamSpecific && %cl.team != %client.team)
            {
                messageClient(%cl, 'OtherTeamVoteStarted',
                    '\c2The other team has voted to kick \c5%1\c2.',
                    %targetClientName);
            }
            else
            {
                messageClient(%cl, 'VoteStarted',
                    '\c2%1 initiated a vote to kick \c5%2\c2.',
                    %client.name, %targetClientName);
            }

        default:
            Game.notifyVote(%cl, %client, %type, %arg1, %arg2, %arg3, %arg4);

        }
        %clientsVoting++;
    }

    for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
    {
        %cl = ClientGroup.getObject(%idx);
        if (%cl.isAIControlled()) continue;
        if (%teamSpecific && %cl.team != %client.team) continue;

        %cl.votee = true;
        messageClient(%cl, 'openVoteHud', "", %clientsVoting,
            ($Host::VotePassPercent / 100));
    }

    // schedule and track information about the vote
    Vote.scheduled = schedule(($Host::VoteTime * 1000), 0,
        "calculateVotes", %client, %type, %arg1, %arg2, %arg3, %arg4);
    Vote.targetAddress = %targetAddress;
    Vote.targetClient = %targetClient;
    Vote.targetClientName = %targetClientName;
    Vote.targetGuid = %targetGuid;
    Vote.targetTeam = %targetTeam;
    Vote.type = %type;

    // register the caller of the vote as for and increment the total count
    %client.vote = true;
    messageAll('addYesVote', "");
    clearBottomPrint(%client);

    // a vote has been scheduled; limit the client from voting for a bit
    if (%admin == 0)
    {
        %client.canVote = false;
        %client.rescheduleVote = schedule(
            ($Host::VoteSpread * 1000) + ($Host::VoteTime * 1000), 0,
            "resetVotePrivs", %client);
    }
}

function calculateVotes(%client, %type, %arg1, %arg2, %arg3, %arg4)
{
    %target = 0;
    %totalVotesAgainst = 0;
    %totalVotesFor = 0;
    %totalVoters = 0;
    for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
    {
        %cl = ClientGroup.getObject(%idx);
        if (%cl.isAIControlled()) continue;

        // verify target exists; always permit target client no matter the team
        if (%cl == Vote.targetClient)
            %target = Vote.targetClient;
        // only consider individuals parts of the initial vote
        else if (!%cl.votee)
            continue;

        %totalVoters++;

        // count votes
        if (%cl.vote !$= "")
        {
            if (%cl.vote)
                %totalVotesFor++;
            else
                %totalVotesAgainst++;
        }
    }

    // calculate if the vote has passed
    %passed = ((%totalVotesFor + %totalVotesAgainst) > 0 &&
            (%totalVotesFor / %totalVoters) > ($Host::VotePassPercent / 100));
    %percentage = mFloor(%totalVotesFor / %totalVoters * 100);

    switch$ (%type)
    {
    case "VoteAdminPlayer":
        if (%target == 0 || %target.isAdmin)
        {
            clearVotes();
            return;
        }

        if (%passed)
        {
            messageAll('MsgAdminPlayer',
                '\c2%2 was made an administrator by vote.',
                %target, %target.name);
            %target.isAdmin = true;
        }
        else
            messageAll('MsgVoteFailed',
                '\c2Vote to make %1 an administrator did not pass.',
                %target.name);

    case "VoteKickPlayer":
        // re-check equivalent access (if target still exists)
        if (%target > 0 && ((%target.isSuperAdmin && !%client.isSuperAdmin) ||
                (%target.isAdmin && !%client.isAdmin)))
        {
            clearVotes();
            return;
        }

        if (%passed)
            kick(Vote.targetClient, 0, Vote.targetGuid, Vote.targetAddress,
                Vote.targetClientName, Vote.targetTeam);
        else if (%specificTeam != 0)
        {
            %msg = "\c2Vote to kick " @ getTaggedString(Vote.targetClientName) @
                    " did not pass (" @ %percentage @ "\%).";
            messageTeam(%specificTeam, 'MsgVoteFailed', %msg);
            if (%target > 0 && %specificTeam != %target.team)
                messageClient(%target, 'MsgVoteFailed', %msg);
        }
        else
            messageAll('MsgVoteFailed',
                "\c2Vote to kick " @ getTaggedString(Vote.targetClientName) @
                    " did not pass (" @ %percentage @ "\%).");

    default:
        Game.processVote(%client, %type, %passed, %percentage,
            %arg1, %arg2, %arg3, %arg4);
    }

    clearVotes();
}

function clearVotes()
{
    for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
    {
        %client = ClientGroup.getObject(%idx);
        %client.vote = "";
        %client.votee = false;
        messageClient(%client, 'closeVoteHud', "");
        messageClient(%client, 'clearVoteHud', "");
    }

    cancel(Vote.scheduled);
    Vote.scheduled = "";
    Vote.type = %type;
    Vote.targetAddress = "";
    Vote.targetClient = 0;
    Vote.targetClientName = "";
    Vote.targetGuid = 0;
    Vote.targetTeam = 0;
}

function resetVotePrivs(%client)
{
    %client.canVote = true;
    %client.rescheduleVote = "";
}

function serverCmdSetPlayerVote(%client, %vote)
{
    // registered voters can only vote once
    if (%client.votee && %client.vote $= "")
    {
        %client.vote = %vote;
        if (%client.vote == 1)
            messageAll('addYesVote', "");
        else
            messageAll('addNoVote', "");

        commandToClient(%client, 'voteSubmitted', %vote);
    }
}
