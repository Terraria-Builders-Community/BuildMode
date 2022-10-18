using Auxiliary;
using Auxiliary.Configuration;
using BuildMode.Entities;
using Microsoft.Xna.Framework;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace BuildMode
{
    [ApiVersion(2, 1)]
    public class BuildMode : TerrariaPlugin
    {
        private System.Timers.Timer _buffTimer;

        private readonly List<int> _enabled = new();
        private readonly bool[] _state = new bool[256];

        private byte[] _surface;
        private byte[] _rock;
        private byte[] _removeBg;

        private double _time;
        private bool _day;

        public override string Author
            => "TBC Developers";

        public override string Description
            => "A plugin that sets fake time by command.";

        public override string Name
            => "Buildmode";

        public override Version Version
            => new(1, 0);

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public BuildMode(Main game)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            : base(game)
        {
            Order = 1;
        }

        public override void Initialize()
        {
            Configuration<BuildModeSettings>.Load("Buildmode");

            GeneralHooks.ReloadEvent += (x) =>
            {
                Configuration<BuildModeSettings>.Load("Buildmode");
                x.Player.SendSuccessMessage("[Buildmode] Reloaded configuration.");
            };

            _buffTimer = new(1000)
            {
                AutoReset = true
            };
            _buffTimer.Elapsed += async (_, x) 
                => await Tick(x);

            _buffTimer.Start();

            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
            ServerApi.Hooks.NetSendBytes.Register(this, OnSendBytes);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);

            Commands.ChatCommands.Add(new Command("buildmode.toggle", async (x) =>
            {
                await Command(x);
            }, "buildmode", "bm"));

            Commands.ChatCommands.Add(new Command("buildmode.time", Time, "stime", "st"));
        }

        private async Task Tick(ElapsedEventArgs _)
        {
            foreach (int i in _enabled)
            {
                var plr = TShock.Players[i];

                if (plr is null || !(plr.Active && plr.IsLoggedIn))
                    continue;

                if (plr.Account is null)
                    continue;

                var entity = await BuffsEntity.GetAsync(plr.Account.ID);

                foreach (var buff in entity.Buffs)
                    plr.SetBuff(buff, 120);
            }
        }

        private void OnLeave(LeaveEventArgs args)
            => _enabled.Remove(args.Who);

        private void OnUpdate(EventArgs args)
        {
            _time++;

            if (!_day && _time > 32400)
            {
                _time = 0;
                _day = true;
            }
            else if (_day && _time > 54000)
            {
                _time = 0;
                _day = false;
            }
        }

        private void OnPostInitialize(EventArgs args)
        {
            _surface = BitConverter.GetBytes((short)Main.worldSurface);
            _rock = BitConverter.GetBytes((short)Main.rockLayer);
            _removeBg = BitConverter.GetBytes((short)Main.maxTilesY);

            _time = Main.time;
            _day = Main.dayTime;
        }

        private void OnSendBytes(SendBytesEventArgs args)
        {
            var plr = TShock.Players[args.Socket.Id];

            if (plr == null)
                return;

            int i = plr.Index;
            bool enabled = _enabled.Contains(i);

            var type = (PacketTypes)args.Buffer[2];
            switch (type)
            {
                case PacketTypes.WorldInfo:
                    {
                        byte[] surface;
                        byte[] rock;

                        int time;
                        bool day;

                        if (enabled)
                        {
                            surface = _removeBg;
                            rock = _removeBg;

                            time = 27000;

                            day = _state[i];
                        }

                        else
                        {
                            surface = _surface;
                            rock = _rock;

                            time = (int)_time;
                            day = _day;
                        }

                        Buffer.BlockCopy(surface, 0, args.Buffer, 17, 2);
                        Buffer.BlockCopy(rock, 0, args.Buffer, 19, 2);

                        Buffer.BlockCopy(BitConverter.GetBytes(time), 0, args.Buffer, 3, 4);
                        args.Buffer[7] = (byte)(day ? 1 : 0);
                    }
                    break;

                case PacketTypes.TimeSet:
                    {
                        int time;
                        bool day;

                        if (enabled)
                        {
                            time = 27000;
                            day = true;
                        }

                        else
                        {
                            time = (int)_time;
                            day = _day;
                        }

                        args.Buffer[3] = (byte)(day ? 1 : 0);
                        Buffer.BlockCopy(BitConverter.GetBytes(time), 0, args.Buffer, 4, 4);
                    }
                    break;
            }
        }

        private async Task Command(CommandArgs args)
        {
            int i = args.Player.Index;
            int id = args.Player.Account.ID;

            var entity = await BuffsEntity.GetAsync(id);

            switch (args.Parameters.FirstOrDefault())
            {
                case "buffs":
                case "buff":
                case "b":
                    args.Parameters.RemoveAt(0);
                    switch (args.Parameters.FirstOrDefault())
                    {
                        case "remove":
                        case "r":
                        case "delete":
                        case "del":
                            {
                                int buffId = 0;
                                if (args.Parameters.Count != 2)
                                    args.Player.SendErrorMessage("Please define a buff to remove");

                                else if (!int.TryParse(args.Parameters[1], out buffId))
                                {
                                    var found = TShock.Utils.GetBuffByName(args.Parameters[1]);

                                    if (found.Count == 0)
                                        args.Player.SendErrorMessage("Invalid buff name!");

                                    else if (found.Count > 1)
                                        args.Player.SendMultipleMatchError(found.Select(f => Lang.GetBuffName(f)));

                                    else
                                        buffId = found[0];

                                }

                                if (buffId > 0 && buffId < Main.maxBuffTypes)
                                {
                                    if (entity.Buffs.Contains(buffId))
                                    {
                                        entity.Buffs = entity.Buffs.Where(x => x != buffId).ToArray();
                                        args.Player.SendSuccessMessage($"Removed {Lang.GetBuffName(buffId)}");
                                    }

                                    else
                                        args.Player.SendErrorMessage("You do not have this buff so it was not removed.");
                                }
                            }
                            return;

                        case "append":
                        case "add":
                        case "a":
                            {
                                int buffId = 0;
                                if (args.Parameters.Count != 2)
                                    args.Player.SendErrorMessage("Please define a buff to add");

                                else if (!int.TryParse(args.Parameters[1], out buffId))
                                {
                                    var found = TShock.Utils.GetBuffByName(args.Parameters[1]);

                                    if (found.Count == 0)
                                        args.Player.SendErrorMessage("Invalid buff name!");

                                    else if (found.Count > 1)
                                        args.Player.SendMultipleMatchError(found.Select(f => Lang.GetBuffName(f)));

                                    else
                                        buffId = found[0];
                                }

                                if (buffId > 0 && buffId < Main.maxBuffTypes)
                                {
                                    if (entity.Buffs.Contains(buffId))
                                        args.Player.SendErrorMessage("You already have this buff!");

                                    else
                                    {
                                        entity.Buffs = entity.Buffs.Concat(new[] { buffId }).ToArray();
                                        args.Player.SendSuccessMessage($"Added {Lang.GetBuffName(buffId)}");
                                    }
                                }

                                else
                                    args.Player.SendErrorMessage("Invalid buff ID!");
                            }
                            return;

                        case "list":
                        case "l":
                            args.Player.SendInfoMessage($"Your current buffs: (Defined by ID)\n{string.Join(", ", entity.Buffs)}");
                            return;
                        default:
                            args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/buildmode buffs (add/remove/list) <buff>");
                            return;
                    }

                case "day":
                case "d":
                case "daytime":
                    if (!_enabled.Contains(i))
                        _enabled.Add(i);
                    _state[i] = true;

                    args.Player.SendData(PacketTypes.WorldInfo);
                    args.Player.SendSuccessMessage("Daytime buildmode activated.");

                    return;

                case "night":
                case "n":
                case "nighttime":
                    if (!_enabled.Contains(i))
                        _enabled.Add(i);
                    _state[i] = false;

                    args.Player.SendData(PacketTypes.WorldInfo);
                    args.Player.SendSuccessMessage("Nighttime buildmode activated.");

                    return;

                case "disable":
                case "dis":
                case "stop":
                case "break":
                    var success = _enabled.Remove(i);

                    if (success)
                    {
                        args.Player.SendData(PacketTypes.WorldInfo);
                        args.Player.SendSuccessMessage("Buildmode has been disabled.");
                    }
                    else
                        args.Player.SendErrorMessage("Buildmode was not active!");

                    return;

                default:
                    args.Player.SendErrorMessage("Invalid syntax. '/bm (day/night/buffs/disable)");
                    return;
            }
        }

        private void Time(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                double time = _time / 3600.0;
                time += 4.5;
                if (!_day)
                    time += 15.0;
                time %= 24.0;
                args.Player.SendInfoMessage("The current time is {0}:{1:D2}.", (int)Math.Floor(time), (int)Math.Floor((time % 1.0) * 60.0));
                return;
            }
            switch (args.Parameters[0].ToLower())
            {
                case "day":
                    SetTime(true, 0.0);
                    args.Player.SendSuccessMessage("You have changed the server time to 4:30 (4:30 AM).");
                    TSPlayer.All.SendMessage(string.Format("{0} has set the server time to 4:30.", args.Player.Name), Color.CornflowerBlue);
                    break;
                case "night":
                    SetTime(false, 0.0);
                    args.Player.SendSuccessMessage("You have changed the server time to 19:30 (7:30 PM).");
                    TSPlayer.All.SendMessage(string.Format("{0} has set the server time to 19:30.", args.Player.Name), Color.CornflowerBlue);
                    break;
                case "noon":
                    SetTime(true, 27000.0);
                    args.Player.SendSuccessMessage("You have changed the server time to 12:00 (12:00 PM).");
                    TSPlayer.All.SendMessage(string.Format("{0} has set the server time to 12:00.", args.Player.Name), Color.CornflowerBlue);
                    break;
                case "midnight":
                    SetTime(false, 16200.0);
                    args.Player.SendSuccessMessage("You have changed the server time to 0:00 (12:00 AM).");
                    TSPlayer.All.SendMessage(string.Format("{0} has set the server time to 0:00.", args.Player.Name), Color.CornflowerBlue);
                    break;
                default:
                    string[] array = args.Parameters[0].Split(':');
                    if (array.Length != 2)
                    {
                        args.Player.SendErrorMessage("Invalid time string! Proper format: hh:mm, in 24-hour time.");
                        return;
                    }

                    int hours;
                    int minutes;
                    if (!int.TryParse(array[0], out hours) || hours < 0 || hours > 23
                        || !int.TryParse(array[1], out minutes) || minutes < 0 || minutes > 59)
                    {
                        args.Player.SendErrorMessage("Invalid time string! Proper format: hh:mm, in 24-hour time.");
                        return;
                    }

                    decimal time = hours + (minutes / 60.0m);
                    time -= 4.50m;
                    if (time < 0.00m)
                        time += 24.00m;

                    if (time >= 15.00m)
                        SetTime(false, (double)((time - 15.00m) * 3600.0m));

                    else
                        SetTime(true, (double)(time * 3600.0m));

                    args.Player.SendSuccessMessage(string.Format("You have changed the server time to {0}:{1:D2}.", hours, minutes));
                    TSPlayer.All.SendMessage(string.Format("{0} set the server time to {1}:{2:D2}.", args.Player.Name, hours, minutes), Color.CornflowerBlue);
                    break;
            }
        }

        private void SetTime(bool dayTime, double time)
        {
            _day = dayTime;
            _time = time;
            TSPlayer.Server.SetTime(dayTime, time);
            TSPlayer.All.SendData(PacketTypes.TimeSet, "", dayTime ? 1 : 0, (int)time, Main.sunModY, Main.moonModY);
        }
    }
}