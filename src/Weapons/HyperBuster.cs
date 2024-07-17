using System;
using System.Collections.Generic;

namespace MMXOnline;

public class HyperBuster : Weapon {
	public const float ammoUsage = 4;
	public const float weaponAmmoUsage = 4;
	MegamanX mmx = null!;

	public HyperBuster() : base() {
		index = (int)WeaponIds.HyperBuster;
		killFeedIndex = 48;
		weaponBarBaseIndex = 32;
		weaponBarIndex = 31;
		weaponSlotIndex = 36;
		shootSounds = new string[] { "", "", "", "" };
		rateOfFire = 2f;
		switchCooldown = 0.25f;
		ammo = 0;
		drawGrayOnLowAmmo = true;
		drawRoundedDown = true;
	}

	public override void update() {
		base.update();
	}

	public override float getAmmoUsage(int chargeLevel) {
		return ammoUsage;
	}

	public float getChipFactoredAmmoUsage(Player player) {
		return player.hasChip(3) ? ammoUsage / 2 : ammoUsage;
	}

	public static float getRateofFireMod(Player player) {
		if (player != null && player.hyperChargeSlot < player.weapons.Count &&
			player.weapons[player.hyperChargeSlot] is Buster && !player.hasUltimateArmor()
		) {
			return 0.75f;
		}
		return 1;
	}

	public float getRateOfFire(Player player) {
		return rateOfFire * getRateofFireMod(player);
	}

	public override bool canShoot(int chargeLevel, Player player) {
		return ammo >= getChipFactoredAmmoUsage(player) && player.weapons[player.hyperChargeSlot].ammo > 0 && shootTime == 0 && player.character?.flag == null;
	}

	public bool canShootIncludeCooldown(Player player) {
		return ammo >= getChipFactoredAmmoUsage(player) && player.weapons.InRange(player.hyperChargeSlot) && player.weapons[player.hyperChargeSlot].ammo > 0;
	}

	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		mmx = player.character as MegamanX ?? throw new NullReferenceException();
		if (player.character.charState is WallSlide) {
			shootTime = 0;
			player.character.playSound("buster3X3", forcePlay: true, sendRpc: true);
			if (!mmx.stockedX3Charge) {
				mmx.stockedX3Charge = true;
				new BusterX3Proj1(
				player.weapon, pos, xDir, 0,
				player, player.getNextActorNetId(), rpc: true);
				}
			else {
				mmx.stockedX3Charge = false;
				Global.serverClient?.rpc(RPC.playerToggle, (byte)player.id, (int)RPCToggleType.UnstockX3Charge);
				new Buster3Proj(
				player.weapon, pos, xDir, 0,
				player, player.getNextActorNetId(), rpc: true);
				}
				return;
			}
		else {
			shootTime = 0;
			}
		player.character.changeState(new X3ChargeShot(null), true);
		return;
	}
}