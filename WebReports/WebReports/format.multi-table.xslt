<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
  <xsl:output method="html" version="4.0"/>
  <xsl:template match="/">
    <htlml>
      <body >
        <div align="center">
          <font size="2" face="Arial">
            <table border="1">
              <xsl:for-each select="/NewDataSet/*" >
                <xsl:if test="name(preceding-sibling::node()[1])!=local-name()" >
                  <xsl:text disable-output-escaping="yes"><![CDATA[</table><br/><br/><table border="1">]]></xsl:text>
                  <tr class="header" bgcolor="#9acd32" >
                    <xsl:for-each select="./*">
                      <td>
                        <xsl:value-of select="local-name()" />
                      </td>
                    </xsl:for-each>
                  </tr>
                </xsl:if>
                <tr class="row" >
                  <xsl:for-each select="./*">
                    <td>
                      <xsl:value-of select="." />
                    </td>
                  </xsl:for-each>
                </tr>
              </xsl:for-each>
            </table>
          </font>
        </div>
      </body>
    </htlml>
  </xsl:template>
</xsl:stylesheet>
