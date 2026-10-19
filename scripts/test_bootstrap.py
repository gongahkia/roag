"""Offline checks for compiler reuse, verified installation, and Intel setup."""
from contextlib import redirect_stdout
import hashlib
import io
import json
import os
from pathlib import Path
import tarfile
import tempfile
import unittest
from unittest.mock import patch
import zipfile

import bootstrap


class BootstrapTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.lock = json.loads(json.dumps(bootstrap.LOCK))
        self.version = f'odin version {self.lock["release"]}:{self.lock["commit"][:7]}'
        self.start(patch.object(bootstrap, 'ROOT', self.root))
        self.start(patch.object(bootstrap, 'LOCK', self.lock))
        self.start(patch.dict(os.environ, {}, clear=True))
        self.system = self.start(patch.object(bootstrap.platform, 'system', return_value='Darwin'))
        self.machine = self.start(patch.object(bootstrap.platform, 'machine', return_value='arm64'))
        self.which = self.start(patch.object(bootstrap.shutil, 'which', return_value=None))
        self.start(patch.object(bootstrap, 'odin_version', side_effect=self.read_version))
        self.urlopen = self.start(patch.object(bootstrap.urllib.request, 'urlopen'))
        self.start(redirect_stdout(io.StringIO()))

    def start(self, context):
        result = context.__enter__()
        self.addCleanup(context.__exit__, None, None, None)
        return result

    def read_version(self, binary):
        path = Path(binary)
        return path.read_text() if path.is_file() else None

    def compiler(self, path, version=None):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(self.version if version is None else version)
        return path

    def archive(self, key='macos-arm64', version=None):
        asset = self.lock['assets'][key]
        name = 'odin.exe' if key.startswith('windows-') else 'odin'
        payload = (self.version if version is None else version).encode()
        output = io.BytesIO()
        if asset['file'].endswith('.zip'):
            with zipfile.ZipFile(output, 'w') as archive:
                archive.writestr(f'Odin/{name}', payload)
                archive.writestr('Odin/core/README', 'bundled libraries')
        else:
            with tarfile.open(fileobj=output, mode='w:gz') as archive:
                for path, body in ((f'Odin/{name}', payload), ('Odin/core/README', b'bundled libraries')):
                    member = tarfile.TarInfo(path)
                    member.size = len(body)
                    archive.addfile(member, io.BytesIO(body))
        data = output.getvalue()
        asset['sha256'] = hashlib.sha256(data).hexdigest()
        self.urlopen.return_value = io.BytesIO(data)

    def test_reuses_matching_local_compiler_on_intel_without_downloading(self):
        self.machine.return_value = 'x86_64'
        binary = self.compiler(self.root / '.tools/odin/odin')
        self.assertEqual(bootstrap.install_odin(), str(binary))
        self.urlopen.assert_not_called()

    def test_reuses_matching_path_compiler_over_stale_local_version(self):
        self.compiler(self.root / '.tools/odin/odin', 'odin version old:0000000')
        installed = self.compiler(self.root / 'system/bin/odin')
        self.which.side_effect = lambda name: str(installed) if name == 'odin' else None
        self.assertEqual(bootstrap.install_odin(), str(installed))
        self.urlopen.assert_not_called()

    def test_explicit_compiler_override_is_validated(self):
        installed = self.compiler(self.root / 'custom/odin')
        os.environ['ODIN_BIN'] = str(installed)
        self.assertEqual(bootstrap.install_odin(), str(installed))
        installed.write_text('odin version old:0000000')
        with self.assertRaisesRegex(SystemExit, 'ODIN_BIN must select Odin'):
            bootstrap.install_odin()
        installed.unlink()
        with self.assertRaisesRegex(SystemExit, 'missing or unusable'):
            bootstrap.install_odin()
        self.urlopen.assert_not_called()

    def test_downloads_verified_archive_for_supported_platforms_and_reuses_it(self):
        cases = (
            ('Darwin', 'arm64', 'macos-arm64', 'odin'),
            ('Windows', 'AMD64', 'windows-amd64', 'odin.exe'),
            ('Linux', 'x86_64', 'linux-amd64', 'odin'),
            ('Linux', 'aarch64', 'linux-arm64', 'odin'),
        )
        for system, machine, key, name in cases:
            with self.subTest(key=key), tempfile.TemporaryDirectory(dir=self.root) as directory:
                with patch.object(bootstrap, 'ROOT', Path(directory)):
                    self.system.return_value, self.machine.return_value = system, machine
                    self.archive(key)
                    binary = Path(bootstrap.install_odin())
                    self.assertEqual(binary.read_text(), self.version)
                    self.assertEqual(binary.name, name)
                    self.assertTrue((binary.parent / 'core/README').is_file())
                    self.urlopen.assert_called_once()
                    self.assertEqual(bootstrap.install_odin(), str(binary))
                    self.urlopen.assert_called_once()
                    self.urlopen.reset_mock()

    def test_checksum_failure_preserves_existing_installation(self):
        existing = self.compiler(
            self.root / '.tools' / f'odin-{self.lock["release"]}' / 'odin', 'old compiler',
        )
        self.archive()
        self.lock['assets']['macos-arm64']['sha256'] = '0' * 64
        with self.assertRaisesRegex(SystemExit, 'SHA-256 mismatch'):
            bootstrap.install_odin()
        self.assertEqual(existing.read_text(), 'old compiler')
        self.assertEqual(list(existing.parent.parent.glob('.odin-install-*')), [])

    def test_replaces_stale_versioned_installation_only_after_validation(self):
        existing = self.compiler(
            self.root / '.tools' / f'odin-{self.lock["release"]}' / 'odin', 'old compiler',
        )
        self.archive(version='wrong compiler')
        with self.assertRaisesRegex(SystemExit, 'does not match the pinned commit'):
            bootstrap.install_odin()
        self.assertEqual(existing.read_text(), 'old compiler')
        self.archive()
        self.assertEqual(bootstrap.install_odin(), str(existing))
        self.assertEqual(existing.read_text(), self.version)

    def test_intel_macos_builds_from_source_when_compiler_is_missing(self):
        self.machine.return_value = 'x86_64'
        with patch.object(bootstrap, 'build_macos_intel') as build_source:
            build_source.side_effect = lambda target: self.compiler(target / 'odin')
            binary = Path(bootstrap.install_odin())
            self.assertEqual(binary.read_text(), self.version)
            build_source.assert_called_once()
        self.urlopen.assert_not_called()

    def test_source_build_checks_full_commit_and_skips_unneeded_lfs_assets(self):
        target = self.root / 'source'
        with patch.object(bootstrap, 'llvm_config', return_value='/llvm/bin/llvm-config'):
            with patch.object(bootstrap.subprocess, 'run') as run:
                with patch.object(bootstrap.subprocess, 'check_output', return_value=self.lock['commit']):
                    bootstrap.build_macos_intel(target)
        clone = run.call_args_list[1]
        self.assertIn('filter.lfs.required=false', clone.args[0])
        self.assertIn(self.lock['release'], clone.args[0])
        self.assertEqual(clone.kwargs['env']['GIT_LFS_SKIP_SMUDGE'], '1')
        self.assertEqual(clone.kwargs['env']['LLVM_CONFIG'], '/llvm/bin/llvm-config')
        self.assertEqual(run.call_args_list[2].args[0], ['make', 'release-native'])
        self.assertEqual(run.call_args_list[2].kwargs['cwd'], target)

    def test_source_commit_mismatch_stops_before_compilation(self):
        with patch.object(bootstrap, 'llvm_config', return_value='/llvm/bin/llvm-config'):
            with patch.object(bootstrap.subprocess, 'run') as run:
                with patch.object(bootstrap.subprocess, 'check_output', return_value='wrong-commit'):
                    with self.assertRaisesRegex(SystemExit, 'source commit mismatch'):
                        bootstrap.build_macos_intel(self.root / 'source')
        self.assertEqual(run.call_count, 2)  # SDK check and clone, never compilation.

    def test_reuses_supported_llvm_without_homebrew(self):
        config = self.compiler(self.root / 'llvm/bin/llvm-config', '22.1.8')
        self.which.side_effect = lambda name: str(config) if name == 'llvm-config' else None
        with patch.object(bootstrap.subprocess, 'check_output', return_value='22.1.8'):
            self.assertEqual(bootstrap.llvm_config(), str(config))

    def test_missing_llvm_is_installed_with_existing_homebrew(self):
        self.which.side_effect = lambda name: '/bin/brew' if name == 'brew' else None
        prefix = self.root / 'brew'
        llvm = prefix / 'opt/llvm@22'
        with patch.object(bootstrap.subprocess, 'check_output', side_effect=[str(prefix), str(llvm)]):
            with patch.object(bootstrap.subprocess, 'run') as run:
                self.assertEqual(bootstrap.llvm_config(), str(llvm / 'bin/llvm-config'))
        run.assert_called_once_with(['/bin/brew', 'install', 'llvm@22'], check=True)

    def test_missing_llvm_without_homebrew_has_actionable_error(self):
        with self.assertRaisesRegex(SystemExit, 'LLVM_CONFIG'):
            bootstrap.llvm_config()

    def test_pin_matching_rejects_longer_hash_and_missing_version(self):
        self.assertTrue(bootstrap.matches_pin(self.version))
        self.assertFalse(bootstrap.matches_pin(self.version + 'a'))
        self.assertFalse(bootstrap.matches_pin(None))


if __name__ == '__main__':
    unittest.main()
