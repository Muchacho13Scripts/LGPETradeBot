﻿using PKHeX.Core;
using PKHeX.Core.Searching;
using SysBot.Base;
using System;
using System.Drawing;
using System.Linq;
using PKHeX.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsets;
using System.Collections;
using System.Collections.Generic;
using Discord;
using System.Diagnostics;


namespace SysBot.Pokemon
{

    public class LetsGoTrades : PokeRoutineExecutor
    {
        
        
        public static SAV7b sav = new();
        public static PB7 pkm = new();
        public static PokeTradeHub<PK8> Hub;
        public static Queue discordname = new();
        public static Queue Channel = new();
        public static Queue discordID = new();
        public static Queue tradepkm = new();
        public LetsGoTrades(PokeTradeHub<PK8> hub, PokeBotState cfg) : base(cfg)
        {
            Hub = hub;
           
        }
        public override async Task MainLoop(CancellationToken token)
        {
            
            Log("Identifying trainer data of the host console.");
          sav = await LGIdentifyTrainer(token).ConfigureAwait(false);

            Log("Starting main TradeBot loop.");
         
                Config.IterateNextRoutine();
                var task = Config.CurrentRoutineType switch
                {
                    
                     PokeRoutineType.LGPETradeBot => DoTrades(token),
                     _ => DoNothing(token)
                };
                await task.ConfigureAwait(false);
            
            await DetachController(token);
            Hub.Bots.Remove(this);
            
        }

        private const int InjectBox = 0;
        private const int InjectSlot = 0;

        public async Task DoNothing(CancellationToken token)
        {
            int waitCounter = 0;
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.Idle)
            {
                if (waitCounter == 0)
                    Log("No task assigned. Waiting for new task assignment.");
                waitCounter++;
                await Task.Delay(1_000, token).ConfigureAwait(false);
            }
        }
        private static readonly byte[] BlackPixel = // 1x1 black pixel
       {
            0x42, 0x4D, 0x3A, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x28, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00
        };
        public async Task DoTrades(CancellationToken token)
        {
            var BoxStart = 0x533675B0;
            var SlotSize = 260;
            var GapSize = 380;
            var SlotCount = 25;
         
            uint GetBoxOffset(int box) => 0x533675B0;
            uint GetSlotOffset(int box, int slot) => GetBoxOffset(box) + (uint)((SlotSize + GapSize) * slot);
            while (!token.IsCancellationRequested)
            {
                int waitCounter = 0;
                while (tradepkm.Count == 0)
                {
                    
                   
                    if (waitCounter == 0)
                        Log("Nothing to check, waiting for new users...") ;
                    waitCounter++;
                    await Task.Delay(1_000, token).ConfigureAwait(false);
                    
                }
                Log("starting a trade sequence");
                var code = new List<pictocodes>();
                for (int i = 0; i <= 2; i++)
                {
                    code.Add((pictocodes)Util.Rand.Next(10));

                }
              //  System.Text.StringBuilder strbui = new System.Text.StringBuilder();
                var pictoembed0 = new EmbedBuilder().WithTitle($"{code[0]}");
                var pictoembed1 = new EmbedBuilder().WithTitle($"{code[1]}");
                var pictoembed2 = new EmbedBuilder().WithTitle($"{code[2]}");
                pictoembed0.ThumbnailUrl = $"https://play.pokemonshowdown.com/sprites/ani/{code[0].ToString().ToLower()}.gif";
                pictoembed1.ThumbnailUrl = $"https://play.pokemonshowdown.com/sprites/ani/{code[1].ToString().ToLower()}.gif";
                pictoembed2.ThumbnailUrl = $"https://play.pokemonshowdown.com/sprites/ani/{code[2].ToString().ToLower()}.gif";
                var user = (IUser)discordname.Peek();
                await user.SendMessageAsync($"My IGN is {Connection.Label.Split('-')[0]}\nHere is your link code:");
                await user.SendMessageAsync(embed: pictoembed0.Build());
                await user.SendMessageAsync(embed: pictoembed1.Build());
                await user.SendMessageAsync(embed: pictoembed2.Build());
                var pkm = (PB7)tradepkm.Peek();
                var slotofs = GetSlotOffset(1, 0);
                var StoredLength = SlotSize- 0x1C;
                await Connection.WriteBytesAsync(pkm.EncryptedBoxData.Slice(0, StoredLength), BoxSlot1,token);
                await Connection.WriteBytesAsync(pkm.EncryptedBoxData.SliceEnd(StoredLength), (uint)(slotofs + StoredLength + 0x70),token);
              
                await Click(X, 200, token).ConfigureAwait(false);
                await Task.Delay(1000).ConfigureAwait(false);
               await SetStick(SwitchStick.RIGHT, 30000, 0, 100, token).ConfigureAwait(false);
                await SetStick(SwitchStick.RIGHT, 0, 0, 100, token).ConfigureAwait(false);
                await Task.Delay(500);
                await Click(A, 200, token).ConfigureAwait(false);
                await Task.Delay(500);
                await Click(A, 200, token).ConfigureAwait(false);
                await Task.Delay(3000).ConfigureAwait(false);
                await SetStick(SwitchStick.RIGHT,0,-30000, 100, token).ConfigureAwait(false);
                await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                await Click(A, 200, token).ConfigureAwait(false);
                await Task.Delay(3000).ConfigureAwait(false);
                await Click(A, 200, token).ConfigureAwait(false);
                await Task.Delay(1000).ConfigureAwait(false);
                Log("Entering Link Code");
                System.IO.File.WriteAllBytes($"{System.IO.Directory.GetCurrentDirectory()}/Block.png", BlackPixel);
                foreach (pictocodes pc in code)
                {
                    if ((int)pc > 4)
                    {
                        await SetStick(SwitchStick.RIGHT, 0, -30000, 100, token).ConfigureAwait(false);
                        await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                    }
                    if ((int)pc <= 4)
                    {
                        for (int i = (int)pc; i > 0; i--)
                        {
                            await SetStick(SwitchStick.RIGHT, 30000, 0, 100, token).ConfigureAwait(false);
                            await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                            await Task.Delay(500).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        for (int i = (int)pc - 5; i > 0; i--)
                        {
                            await SetStick(SwitchStick.RIGHT, 30000, 0, 100, token).ConfigureAwait(false);
                            await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                            await Task.Delay(500).ConfigureAwait(false);
                        }
                    }
                    await Click(A, 200, token).ConfigureAwait(false);
                    await Task.Delay(500).ConfigureAwait(false);
                    if ((int)pc <= 4)
                    {
                        for (int i = (int)pc; i > 0; i--)
                        {
                            await SetStick(SwitchStick.RIGHT, -30000, 0, 100, token).ConfigureAwait(false);
                            await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                            await Task.Delay(500).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        for (int i = (int)pc - 5; i > 0; i--)
                        {
                            await SetStick(SwitchStick.RIGHT, -30000, 0, 100, token).ConfigureAwait(false);
                            await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                            await Task.Delay(500).ConfigureAwait(false);
                        }
                    }

                    if ((int)pc > 4)
                    {
                        await SetStick(SwitchStick.RIGHT, 0, 30000, 100, token).ConfigureAwait(false);
                        await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                    }
                }
                Log($"Searching for user {discordname.Peek()}");
                await user.SendMessageAsync("searching for you now, you have 30 seconds to match").ConfigureAwait(false);
                await Task.Delay(30_000).ConfigureAwait(false);
                Log("30 seconds are up, hopefully weve matched");
                System.IO.File.Delete($"{System.IO.Directory.GetCurrentDirectory()}/Block.png");
                await Click(A, 200, token).ConfigureAwait(false);
                await Task.Delay(500);
                await Click(A, 200, token).ConfigureAwait(false);
                await user.SendMessageAsync("assuming we have matched,You have 15 seconds to select your trade pokemon");
                Log("waiting on trade screen");
                await Task.Delay(15_000).ConfigureAwait(false);
                await Click(A, 200, token).ConfigureAwait(false);
                await user.SendMessageAsync("trading...");
                Log("trading...");
                await Task.Delay(10000);
                while (await LGIsInTrade(token))
                    await Task.Delay(25);
                await Task.Delay(5000);
                Log("Trade should be completed, backing out");
                await Click(B, 200, token);
                await Task.Delay(500);
                await Click(A, 200, token).ConfigureAwait(false);
                await Task.Delay(500);
               Stopwatch btimeout = new();
                btimeout.Restart();
                int acount = 3;
                while (btimeout.ElapsedMilliseconds < 30_000)
                {
                   await Click(B, 200, token).ConfigureAwait(false);
                   await Task.Delay(1000).ConfigureAwait(false);
                    if(acount == 4)
                    {
                        await Click(A, 200, token);
                        await Task.Delay(500);
                        acount = 0;
                        continue;
                    }
                   acount++;
                }
                await Task.Delay(500);
                await Click(B, 200, token).ConfigureAwait(false);
                await Task.Delay(500);
                await Click(B, 200, token).ConfigureAwait(false);
                await Task.Delay(500);
                await Click(B, 200, token).ConfigureAwait(false);

                btimeout.Stop();
             
                var returnpk = await LGReadPokemon(BoxSlot1, token);
                if (returnpk == null)
                {
                    returnpk = new PB7();
                }
                if (SearchUtil.HashByDetails(returnpk) != SearchUtil.HashByDetails(pkm))
               {
                    byte[] writepoke = returnpk.EncryptedBoxData;
                    var tpfile = System.IO.Path.GetTempFileName().Replace(".tmp", "." + returnpk.Extension);
                    tpfile = tpfile.Replace("tmp", returnpk.FileNameWithoutExtension);
                    System.IO.File.WriteAllBytes(tpfile, writepoke);
                    await user.SendFileAsync(tpfile, "here is the pokemon you traded me");
                    Log($"{discordname.Peek()} completed their trade");
               }
                else
                {
                    await user.SendMessageAsync("Something went wrong, no trade happened, please try again!");
                    Log($"{discordname.Peek()} did not complete their trade");
                }
                discordID.Dequeue();
                discordname.Dequeue();
                Channel.Dequeue();
                tradepkm.Dequeue();
                continue;

            }
        }

        public enum pictocodes
        {
            Pikachu,
            Eevee, 
            Bulbasaur,
            Charmander,
            Squirtle,
            Pidgey,
            Caterpie,
            Rattata,
            Jigglypuff,
            Diglett
        }
    }
}
