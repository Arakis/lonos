# -*- coding: utf-8 -*-

from __future__ import division, print_function, unicode_literals

import os
import sys

import sphinx_rtd_theme
from recommonmark.parser import CommonMarkParser

sys.path.insert(0, os.path.abspath('sphinx'))
sys.path.insert(0, os.path.abspath('..'))
sys.path.append(os.path.dirname(__file__))

sys.path.append(os.path.abspath('_ext'))
extensions = [
    'sphinx.ext.autosectionlabel',
    'sphinx.ext.autodoc',
    'sphinx.ext.intersphinx',
    'sphinxcontrib.httpdomain',
    'sphinx_tabs.tabs',
    'sphinx-prompt',
	 'rstFlatTable',
	 'sphinx.ext.graphviz'
]
templates_path = ['_templates']

source_suffix = ['.rst', '.md']
source_parsers = {
    '.md': CommonMarkParser,
}

master_doc = 'index'
project = u'Abanu Documentation'
copyright = '2008-{}, Abanu Project & contributors'.format(
    2019
)

exclude_patterns = ['_build']
default_role = 'code'
intersphinx_mapping = {
    'abanu': ('http://docs.abanu.io/en/latest/', None),
}
htmlhelp_basename = 'AbanuDoc'
latex_documents = [
    ('index', 'abanu.tex', u'Abanu Project Documentation',
     u'', 'manual'),
]
man_pages = [
    ('index', 'abanu', u'Abanu Project Documentation',
     [u''], 1)
]

exclude_patterns = [
    # 'api' # needed for ``make gettext`` to not die.
]

html_theme = 'sphinx_rtd_theme'
html_static_path = ['_static']
html_theme_path = [sphinx_rtd_theme.get_html_theme_path()]
#html_logo = 'img/logo.svg'
#html_theme_options = {
#    'logo_only': True,
#    'display_version': False,
#}

# Activate autosectionlabel plugin
autosectionlabel_prefix_document = True


def setup(app):
    app.add_stylesheet('css/sphinx_prompt_css.css')

pygments_style = 'sphinx'