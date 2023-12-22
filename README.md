# Požadavky na trénování (jiné verze mohou, ale nemusí fungovat)
- Linux
- Python 3.8
- Godot RL Agents (verze 0.6.2 se Sample Factory frameworkem)
    - `pip install godot-rl[sf]==0.6.2`
    - viz. https://github.com/edbeeching/godot_rl_agents/blob/main/docs/ADV_SAMPLE_FACTORY.md
# Postup při trénování
1. Exportovat hru (vyžaduje godot 4)
    - Ve složce Export/Linux je již exportovaná aktuální verze hry
2. Úprava Godot RL Agents (GDRL)
    - Používám upravenou verzi GDRL
    - Ve složce Export/Linux je soubor `godot_env.py`, nahraďte jím soubor umístěný v knihovnách pythonu: `python3.x/dist-packages/godot_rl/core/godot_env.py`
3. Příkazem `gdrl` spustíte učení
    - Parametry z GDRL: příkaz `gdrl -h`
    - Parametry ze Sample Factory: https://www.samplefactory.dev/02-configuration/cfg-params
    - Příklad: `gdrl --experiment=VsStatic11 --trainer=sf --env=gdrl --env_path=./BombermanRL --speedup=4 --batched_sampling=False --device=cpu --train_for_env_steps=100000000 --recurrence=32 --rollout=64 --num_workers=6 --viz`
4. Spuštění již naučeného modelu
    - Export modelu momentálně není podporovaný, použijte `--eval` u `gdrl`