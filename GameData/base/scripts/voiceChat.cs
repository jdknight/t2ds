//------------------------------------------------------------------------------
function serverCmdListenTo(%client, %who, %boolean)
{
   if ( %client == %who )
      return;

   %client.listenTo( %who, %boolean );

   if ( %echo )
   {
      if ( %boolean )
         messageClient( %client, 'MsgVoiceEnable', 'You will now listen to %3.', %boolean, %who, %who.name );
      else
         messageClient( %client, 'MsgVoiceEnable', 'You will no longer listen to %3.', %boolean, %who, %who.name );
   }
   else
      messageClient( %client, 'MsgVoiceEnable', "", %boolean, %who );

   messageClient( %who, 'MsgListenState', "", %boolean, %client );
}   

//------------------------------------------------------------------------------
function serverCmdListenToAll(%client)
{
   %client.listenToAll();

   for ( %i = 0; %i < ClientGroup.getCount(); %i++ )
   {
      %cl = ClientGroup.getObject( %i );
      if ( %cl && %cl != %client && !%cl.isAIControlled() )
         messageClient( %client, 'MsgVoiceEnable', "", true, %cl );
   }

   messageAllExcept( %client, 'MsgListenState', "", true, %client );
}   

//------------------------------------------------------------------------------
function serverCmdListenToNone(%client)
{
   %client.listenToNone();

   for ( %i = 0; %i < ClientGroup.getCount(); %i++ )
   {
      %cl = ClientGroup.getObject( %i );
      if ( %cl && %cl != %client && !%cl.isAIControlled() )
         messageClient( %client, 'MsgVoiceEnable', "", false, %cl );
   }

   messageAllExcept( %client, 'MsgListenState', "", false, %client );
}   

//------------------------------------------------------------------------------
function serverCmdSetVoiceInfo(%client, %channels, %decodingMask, %encodingLevel)
{
   %wasEnabled = %client.listenEnabled();

   // server has voice comm turned off?
   if($Audio::maxVoiceChannels == 0)
      %decodingMask = 0;
   else
      %decodingMask &= (1 << ($Audio::maxEncodingLevel + 1)) - 1;

   if($Audio::maxEncodingLevel < %encodingLevel)
      %encodingLevel = $Audio::maxEncodingLevel;

   if($Audio::maxVoiceChannels < %channels)
      %channels = $Audio::maxVoiceChannels; 

   %client.setVoiceChannels(%channels);
   %client.setVoiceDecodingMask(%decodingMask);
   %client.setVoiceEncodingLevel(%encodingLevel);

   commandToClient(%client, 'SetVoiceInfo', %channels, %decodingMask, %encodingLevel);

   if ( %wasEnabled != ( %channels > 0 ) )
      updateCanListenState( %client );
}

//------------------------------------------------------------------------------
// SERVER side update function:
//------------------------------------------------------------------------------
function updateCanListenState( %client )
{
   // These can never listen, so they don't need to be updated:
   if ( %client.isAIControlled() || !%client.listenEnabled() )
      return;

   for ( %i = 0; %i < ClientGroup.getCount(); %i++ )
   {
      %cl = ClientGroup.getObject( %i );
      if ( %cl && %cl != %client && !%cl.isAIControlled() )
      {
         messageClient( %cl, 'MsgCanListen', "", %client.canListenTo( %cl ), %client );
         messageClient( %client, 'MsgCanListen', "", %cl.canListenTo( %client ), %cl );
      }
   }
}
